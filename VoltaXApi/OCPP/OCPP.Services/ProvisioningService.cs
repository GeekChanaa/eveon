using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.Data;
using VoltaXApi.Data.Seeders;
using VoltaXApi.Dtos;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Exceptions;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// First-connection onboarding of an OCPP 2.0.1 charge point: discover its device model,
    /// send the settings profile with SetVariables, then reboot it (if a setting needs it) or ask
    /// for a new BootNotification so it leaves the Pending state.
    /// </summary>
    public class ProvisioningService
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ReportTimeout = TimeSpan.FromSeconds(90);
        // Used when the charger did not report DeviceDataCtrlr.ItemsPerMessage[SetVariables].
        private const int DefaultItemsPerMessage = 10;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Converters = new List<JsonConverter> { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly VoltaXApiDbContext _db;
        private readonly OCPPMessageProcessor _messageProcessor;
        private readonly OCPPMessageFactory _messageFactory = new();
        private readonly ReportCompletionTracker _reportTracker;
        private readonly ChargePointStatusManagerService _chargePoints;
        private readonly ILogger<ProvisioningService> _logger;

        public ProvisioningService(
            VoltaXApiDbContext db,
            OCPPMessageProcessor messageProcessor,
            ReportCompletionTracker reportTracker,
            ChargePointStatusManagerService chargePoints,
            ILogger<ProvisioningService> logger)
        {
            _db = db;
            _messageProcessor = messageProcessor;
            _reportTracker = reportTracker;
            _chargePoints = chargePoints;
            _logger = logger;
        }

        public static Task<bool> IsProvisioned(VoltaXApiDbContext db, int chargePointId) =>
            db.ChargePointProvisionings.AnyAsync(p => p.ChargePointID == chargePointId);

        public async Task<List<PendingProvisioningChargePointDto>> GetPending()
        {
            var candidates = await _db.ChargePoints.AsNoTracking()
                .Where(cp => !_db.ChargePointProvisionings.Any(p => p.ChargePointID == cp.ID))
                .Select(cp => new PendingProvisioningChargePointDto
                {
                    ID = cp.ID,
                    ChargePointId = cp.ChargePointId,
                    StationName = cp.ChargingStation != null ? cp.ChargingStation.Name : null,
                    VendorName = cp.VendorName,
                    ModelName = cp.ChargePointModel != null ? cp.ChargePointModel.Name : null,
                    SerialNumber = cp.SerialNumber,
                    FirmwareVersion = cp.FirmwareVersion
                })
                .ToListAsync();

            // Only the ones that are connected now can be configured.
            return candidates.Where(cp => IsOnline(cp.ChargePointId)).ToList();
        }

        /// <summary>Every charge point with its connection and configuration state.</summary>
        public async Task<List<ChargePointConfigurationStatusDto>> GetOverview()
        {
            var list = await StatusQuery().OrderBy(s => s.ChargePointId).ToListAsync();
            foreach (var status in list) status.IsOnline = IsOnline(status.ChargePointId);
            return list;
        }

        public async Task<ChargePointConfigurationStatusDto> GetStatus(string chargePointId)
        {
            var status = await StatusQuery().FirstOrDefaultAsync(s => s.ChargePointId == chargePointId)
                ?? throw new ChargePointNotFoundException($"There is no charge point with the id {chargePointId}");
            status.IsOnline = IsOnline(chargePointId);
            return status;
        }

        private IQueryable<ChargePointConfigurationStatusDto> StatusQuery() =>
            _db.ChargePoints.AsNoTracking().Select(cp => new
            {
                cp,
                provisioning = _db.ChargePointProvisionings.FirstOrDefault(p => p.ChargePointID == cp.ID)
            })
            .Select(x => new ChargePointConfigurationStatusDto
            {
                ID = x.cp.ID,
                ChargePointId = x.cp.ChargePointId,
                StationName = x.cp.ChargingStation != null ? x.cp.ChargingStation.Name : null,
                VendorName = x.cp.VendorName,
                ModelName = x.cp.ChargePointModel != null ? x.cp.ChargePointModel.Name : null,
                SerialNumber = x.cp.SerialNumber,
                FirmwareVersion = x.cp.FirmwareVersion,
                IsConfigured = x.provisioning != null
                    && (x.provisioning.Method == ChargePointProvisioningMethodEnum.Automatic || x.provisioning.Method == ChargePointProvisioningMethodEnum.Manual),
                IsAccepted = x.provisioning != null,
                ConfigurationMethod = x.provisioning != null ? x.provisioning.Method : null,
                ConfigurationStatus = x.provisioning != null ? x.provisioning.Status : null,
                ConfiguredAt = x.provisioning != null ? x.provisioning.ProvisionedAt : null,
                ConfiguredBy = x.provisioning != null && x.provisioning.ProvisionedByUser != null
                    ? x.provisioning.ProvisionedByUser.FirstName + " " + x.provisioning.ProvisionedByUser.LastName : null,
                AcceptedCount = x.provisioning != null ? x.provisioning.AcceptedCount : 0,
                FailedCount = x.provisioning != null ? x.provisioning.FailedCount : 0,
                ReportedVariableCount = _db.OCPPConfigurationItems.Count(i => i.ChargePointID == x.cp.ID),
                LastReportAt = _db.OCPPConfigurationItems.Where(i => i.ChargePointID == x.cp.ID).Max(i => (DateTime?)i.UpdatedAt)
            });

        /// <summary>The charger's device model as last reported (NotifyReport), plus values confirmed since.</summary>
        public async Task<List<DeviceModelVariableDto>> GetDeviceModel(string chargePointId)
        {
            var chargePoint = await FindChargePoint(chargePointId);
            var items = await _db.OCPPConfigurationItems.AsNoTracking()
                .Where(i => i.ChargePointID == chargePoint.ID)
                .Include(i => i.OCPPConfigurationComponent).ThenInclude(c => c!.OCPPConfigurationEVSE)
                .Include(i => i.OCPPConfigurationVariable)
                .Include(i => i.OCPPConfigurationVariableAttributes)
                .Include(i => i.OCPPConfigurationVariableCharacteristic)
                .ToListAsync();

            return items
                .Where(i => i.OCPPConfigurationComponent != null && i.OCPPConfigurationVariable != null)
                .Select(i => new DeviceModelVariableDto
                {
                    ComponentName = i.OCPPConfigurationComponent!.Name,
                    ComponentInstance = i.OCPPConfigurationComponent.Instance,
                    EvseId = i.OCPPConfigurationComponent.OCPPConfigurationEVSE?.EVSEId,
                    ConnectorId = i.OCPPConfigurationComponent.OCPPConfigurationEVSE?.ConnectorId,
                    VariableName = i.OCPPConfigurationVariable!.Name,
                    VariableInstance = i.OCPPConfigurationVariable.Instance,
                    DataType = i.OCPPConfigurationVariableCharacteristic?.DataType.ToString(),
                    Unit = i.OCPPConfigurationVariableCharacteristic?.Unit,
                    MinLimit = i.OCPPConfigurationVariableCharacteristic?.MinLimit,
                    MaxLimit = i.OCPPConfigurationVariableCharacteristic?.MaxLimit,
                    ValuesList = i.OCPPConfigurationVariableCharacteristic?.ValuesList,
                    SupportsMonitoring = i.OCPPConfigurationVariableCharacteristic?.SupportsMonitoring ?? false,
                    UpdatedAt = (i.OCPPConfigurationVariableAttributes ?? new List<OCPPConfigurationVariableAttribute>())
                        .Select(a => a.UpdatedAt).DefaultIfEmpty(i.UpdatedAt).Max(),
                    Attributes = (i.OCPPConfigurationVariableAttributes ?? new List<OCPPConfigurationVariableAttribute>())
                        .OrderBy(a => a.Type ?? AttributeEnumType.Actual)
                        .Select(a => new DeviceModelAttributeDto
                        {
                            Type = (a.Type ?? AttributeEnumType.Actual).ToString(),
                            Value = a.Value,
                            Mutability = (a.Mutability ?? MutabilityEnumType.ReadWrite).ToString(),
                            Persistent = a.Persistent ?? false,
                            Constant = a.Constant ?? false
                        }).ToList()
                })
                .OrderBy(v => v.ComponentName).ThenBy(v => v.ComponentInstance).ThenBy(v => v.EvseId).ThenBy(v => v.ConnectorId)
                .ThenBy(v => v.VariableName).ThenBy(v => v.VariableInstance)
                .ToList();
        }

        public async Task<List<ProvisioningPlanVariableDto>> GetDefaultProfile()
        {
            await OcppDefaultProfileSeeder.Seed(_db);
            return (await _db.OcppDefaultVariables.AsNoTracking()
                .Where(v => v.Enabled)
                .OrderBy(v => v.SortOrder).ThenBy(v => v.ID)
                .ToListAsync())
            .Select(v => new ProvisioningPlanVariableDto
            {
                GroupName = v.GroupName,
                ComponentName = v.ComponentName,
                ComponentInstance = v.ComponentInstance,
                EvseId = v.EvseId,
                ConnectorId = v.ConnectorId,
                VariableName = v.VariableName,
                VariableInstance = v.VariableInstance,
                AttributeType = v.AttributeType.ToString(),
                Value = v.Value,
                Description = v.Description
            })
            .ToList();
        }

        /// <summary>
        /// Asks the charger for its full device model (GetBaseReport FullInventory), waits for the
        /// whole report, and returns the default profile annotated with what the charger supports.
        /// </summary>
        public async Task<ProvisioningPlanDto> Discover(string chargePointId)
        {
            var chargePoint = await FindChargePoint(chargePointId);
            var report = await RequestReport(chargePointId, ReportBaseEnumType.FullInventory);
            var plan = new ProvisioningPlanDto
            {
                ChargePointId = chargePointId,
                DiscoveryStatus = report.Status,
                DiscoveryMessage = report.Message
            };

            var reported = await LoadReportedVariables(chargePoint.ID);
            plan.ReportedVariableCount = reported.Count;
            plan.ItemsPerMessage = ItemsPerMessage(reported);

            // Without any report data, nothing can be marked unsupported.
            var haveReport = reported.Count > 0;
            plan.Variables = await GetDefaultProfile();
            foreach (var variable in plan.Variables)
                Annotate(variable, reported, haveReport);

            return plan;
        }

        /// <summary>
        /// Sends GetBaseReport and waits until the charger has delivered every NotifyReport part.
        /// The report itself is stored by NotifyReportHandler.
        /// </summary>
        public async Task<DeviceModelReportResultDto> RequestReport(string chargePointId, ReportBaseEnumType reportBase)
        {
            var chargePoint = await FindChargePoint(chargePointId);
            var outcome = new DeviceModelReportResultDto { ReportBase = reportBase.ToString() };

            var requestId = Random.Shared.Next(1, int.MaxValue);
            var reportDone = _reportTracker.Register(chargePointId, requestId);
            try
            {
                var request = new GetBaseReportRequest { RequestId = requestId, ReportBase = reportBase };
                var payload = await _messageProcessor.SendRequestAsync(
                    _messageFactory.CreateMessage("GetBaseReport", request), chargePointId, RequestTimeout);
                var response = JsonConvert.DeserializeObject<GetBaseReportResponse>(payload, JsonSettings);

                if (response?.Status is GenericDeviceModelStatusEnumType.Accepted)
                {
                    try
                    {
                        await reportDone.WaitAsync(ReportTimeout);
                    }
                    catch (TimeoutException)
                    {
                        outcome.Status = "TimedOut";
                        outcome.Message = $"The charger did not finish its report within {ReportTimeout.TotalSeconds:0} s. What it sent so far is used.";
                    }
                }
                else
                {
                    outcome.Status = response?.Status == GenericDeviceModelStatusEnumType.NotSupported ? "NotSupported" : "Rejected";
                    outcome.Message = $"The charger answered {response?.Status} to GetBaseReport.";
                }
            }
            catch (Exception ex) when (ex is TimeoutException or OcppCallErrorException)
            {
                outcome.Status = "Failed";
                outcome.Message = ex.Message;
            }
            finally
            {
                _reportTracker.Forget(chargePointId, requestId);
            }

            outcome.ReportedVariableCount = await _db.OCPPConfigurationItems.CountAsync(i => i.ChargePointID == chargePoint.ID);
            return outcome;
        }

        public async Task<ProvisioningResultDto> Apply(string chargePointId, ApplyProvisioningDto request, int? userId)
        {
            var chargePoint = await FindChargePoint(chargePointId);
            var method = Enum.TryParse<ChargePointProvisioningMethodEnum>(request.Method, true, out var parsed) && parsed is ChargePointProvisioningMethodEnum.Automatic or ChargePointProvisioningMethodEnum.Manual
                ? parsed
                : ChargePointProvisioningMethodEnum.Manual;

            var sent = await SendVariables(chargePointId, request.Variables);
            var result = new ProvisioningResultDto
            {
                Results = sent.Results,
                AcceptedCount = sent.AcceptedCount,
                FailedCount = sent.FailedCount,
                RebootRequired = sent.RebootRequired
            };

            if (!sent.AnyAnswer)
            {
                result.Message = "The charger did not answer any SetVariables request. It stays pending; try again.";
                return result;
            }

            await SaveProvisioning(chargePoint.ID,
                result.RebootRequired ? ChargePointProvisioningStatusEnum.AwaitingReboot : ChargePointProvisioningStatusEnum.Provisioned,
                method, userId, result.AcceptedCount, result.FailedCount);
            result.Provisioned = true;
            result.FollowUp = result.RebootRequired
                ? await SendReset(chargePointId)
                : await TriggerBootNotification(chargePointId);
            return result;
        }

        /// <summary>
        /// Sends the variables with SetVariables, in batches the charger accepts, and stores every
        /// accepted value in the saved device model so the dashboard shows it without a new report.
        /// </summary>
        public async Task<DeviceModelSetResultDto> SendVariables(string chargePointId, List<ProvisioningVariableDto> variables)
        {
            var chargePoint = await FindChargePoint(chargePointId);
            var reported = await LoadReportedVariables(chargePoint.ID);
            var result = new DeviceModelSetResultDto();

            foreach (var batch in Batches(variables, ItemsPerMessage(reported)))
            {
                var setRequest = new SetVariablesRequest
                {
                    SetVariableData = batch.Select(ToSetVariableData).ToList()
                };

                try
                {
                    var payload = await _messageProcessor.SendRequestAsync(
                        _messageFactory.CreateMessage("SetVariables", setRequest, JsonSettings), chargePointId, RequestTimeout);
                    var response = JsonConvert.DeserializeObject<SetVariablesResponse>(payload, JsonSettings);
                    result.AnyAnswer = true;
                    result.Results.AddRange(MatchResults(batch, response?.setVariableResult ?? new()));
                }
                catch (Exception ex) when (ex is TimeoutException or OcppCallErrorException)
                {
                    _logger.LogWarning(ex, "{ChargePointId}: SetVariables batch failed", chargePointId);
                    var status = ex is TimeoutException ? "NoResponse" : "CallError";
                    result.Results.AddRange(batch.Select(v => ToResult(v, status, ex.Message)));
                }
            }

            result.AcceptedCount = result.Results.Count(r => r.Status is "Accepted" or "RebootRequired");
            result.FailedCount = result.Results.Count - result.AcceptedCount;
            result.RebootRequired = result.Results.Any(r => r.Status == "RebootRequired");

            await StoreValues(chargePoint.ID, result.Results.Where(r => r.Status is "Accepted" or "RebootRequired").ToList());
            return result;
        }

        /// <summary>Reads current values straight from the charger with GetVariables.</summary>
        public async Task<List<ProvisioningVariableResultDto>> ReadVariables(string chargePointId, List<ProvisioningVariableDto> variables)
        {
            var chargePoint = await FindChargePoint(chargePointId);
            var reported = await LoadReportedVariables(chargePoint.ID);
            var results = new List<ProvisioningVariableResultDto>();

            foreach (var batch in Batches(variables, ItemsPerMessage(reported, "GetVariables")))
            {
                var getRequest = new GetVariablesRequest
                {
                    GetVariableData = batch.Select(v => new GetVariableDataType
                    {
                        AttributeType = ParseAttribute(v.AttributeType),
                        Component = ToComponent(v),
                        Variable = ToVariable(v)
                    }).ToList()
                };

                try
                {
                    var payload = await _messageProcessor.SendRequestAsync(
                        _messageFactory.CreateMessage("GetVariables", getRequest, JsonSettings), chargePointId, RequestTimeout);
                    var answers = JsonConvert.DeserializeObject<GetVariablesResponse>(payload, JsonSettings)?.GetVariableResult ?? new();
                    for (var i = 0; i < batch.Count; i++)
                    {
                        var v = batch[i];
                        var answer = answers.FirstOrDefault(a =>
                                string.Equals(a.Component?.Name, v.ComponentName, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(a.Component?.Instance ?? "", v.ComponentInstance ?? "", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(a.Variable?.Name, v.VariableName, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(a.Variable?.Instance ?? "", v.VariableInstance ?? "", StringComparison.OrdinalIgnoreCase))
                            ?? (i < answers.Count ? answers[i] : null);
                        var result = ToResult(v, answer?.AttributeStatus.ToString() ?? "NoResponse", answer?.AttributeStatusInfo?.ReasonCode);
                        if (answer?.AttributeStatus == GetVariableStatusEnumType.Accepted) result.Value = answer.AttributeValue ?? "";
                        results.Add(result);
                    }
                }
                catch (Exception ex) when (ex is TimeoutException or OcppCallErrorException)
                {
                    var status = ex is TimeoutException ? "NoResponse" : "CallError";
                    results.AddRange(batch.Select(v => ToResult(v, status, ex.Message)));
                }
            }

            await StoreValues(chargePoint.ID, results.Where(r => r.Status == "Accepted").ToList());
            return results;
        }

        /// <summary>Reboots the charger. OnIdle waits for running sessions to end.</summary>
        public async Task<string> Reboot(string chargePointId, bool immediate)
        {
            await FindChargePoint(chargePointId);
            var payload = await _messageProcessor.SendRequestAsync(
                _messageFactory.CreateMessage("Reset", new ResetRequest { Type = immediate ? ResetEnumType.Immediate : ResetEnumType.OnIdle }), chargePointId, RequestTimeout);
            return JsonConvert.DeserializeObject<ResetResponse>(payload, JsonSettings)?.Status.ToString() ?? "Unknown";
        }

        /// <summary>Accepts the charge point with its current settings.</summary>
        public async Task<ProvisioningResultDto> Skip(string chargePointId, int? userId)
        {
            var chargePoint = await FindChargePoint(chargePointId);
            await SaveProvisioning(chargePoint.ID, ChargePointProvisioningStatusEnum.Provisioned,
                ChargePointProvisioningMethodEnum.Skipped, userId, 0, 0);

            return new ProvisioningResultDto
            {
                Provisioned = true,
                FollowUp = IsOnline(chargePointId) ? await TriggerBootNotification(chargePointId) : null,
                Message = "Accepted without changing its settings."
            };
        }

        private async Task<ChargePoint> FindChargePoint(string chargePointId) =>
            await _db.ChargePoints.AsNoTracking().FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointId)
            ?? throw new ChargePointNotFoundException($"There is no charge point with the id {chargePointId}");

        // Open on any instance, not only this one (scale-out).
        private bool IsOnline(string chargePointId) => _chargePoints.ChargePointExists(chargePointId);

        private async Task SaveProvisioning(int chargePointId, ChargePointProvisioningStatusEnum status,
            ChargePointProvisioningMethodEnum method, int? userId, int accepted, int failed)
        {
            var provisioning = await _db.ChargePointProvisionings.FirstOrDefaultAsync(p => p.ChargePointID == chargePointId);
            if (provisioning == null)
            {
                provisioning = new ChargePointProvisioning { ChargePointID = chargePointId };
                _db.ChargePointProvisionings.Add(provisioning);
            }

            provisioning.Status = status;
            provisioning.Method = method;
            provisioning.ProvisionedAt = DateTime.UtcNow;
            provisioning.ProvisionedByUserID = userId;
            provisioning.AcceptedCount = accepted;
            provisioning.FailedCount = failed;
            await _db.SaveChangesAsync();
        }

        private async Task<string> SendReset(string chargePointId)
        {
            try
            {
                // Nothing can be charging: the charger is still in Pending.
                var payload = await _messageProcessor.SendRequestAsync(
                    _messageFactory.CreateMessage("Reset", new ResetRequest { Type = ResetEnumType.Immediate }), chargePointId, RequestTimeout);
                var response = JsonConvert.DeserializeObject<ResetResponse>(payload, JsonSettings);
                return $"Reset: {response?.Status}";
            }
            catch (Exception ex) when (ex is TimeoutException or OcppCallErrorException or WebSocketNotFoundException)
            {
                return "Reset could not be sent: " + ex.Message + " Reboot the charger to apply the settings.";
            }
        }

        private async Task<string> TriggerBootNotification(string chargePointId)
        {
            try
            {
                var request = new TriggerMessageRequest { RequestedMessage = MessageTriggerEnumType.BootNotification, Evse = null! };
                var payload = await _messageProcessor.SendRequestAsync(
                    _messageFactory.CreateMessage("TriggerMessage", request), chargePointId, RequestTimeout);
                var response = JsonConvert.DeserializeObject<TriggerMessageResponse>(payload, JsonSettings);
                return $"BootNotification requested: {response?.Status}";
            }
            catch (Exception ex) when (ex is TimeoutException or OcppCallErrorException or WebSocketNotFoundException)
            {
                // Harmless: a pending charger sends a new BootNotification after its retry interval anyway.
                return "BootNotification not triggered (" + ex.Message + "). The charger will be accepted on its next boot retry.";
            }
        }

        // ---------------------------------------------------------------- device model data

        private sealed record ReportedVariable(string Component, string? ComponentInstance, string Variable, string? VariableInstance,
            List<OCPPConfigurationVariableAttribute> Attributes, OCPPConfigurationVariableCharacteristic? Characteristic);

        private async Task<List<ReportedVariable>> LoadReportedVariables(int chargePointId) =>
            (await _db.OCPPConfigurationItems.AsNoTracking()
                .Where(i => i.ChargePointID == chargePointId)
                .Include(i => i.OCPPConfigurationComponent)
                .Include(i => i.OCPPConfigurationVariable)
                .Include(i => i.OCPPConfigurationVariableAttributes)
                .Include(i => i.OCPPConfigurationVariableCharacteristic)
                .ToListAsync())
            .Where(i => i.OCPPConfigurationComponent != null && i.OCPPConfigurationVariable != null)
            .Select(i => new ReportedVariable(
                i.OCPPConfigurationComponent!.Name, i.OCPPConfigurationComponent.Instance,
                i.OCPPConfigurationVariable!.Name, i.OCPPConfigurationVariable.Instance,
                i.OCPPConfigurationVariableAttributes?.ToList() ?? new(), i.OCPPConfigurationVariableCharacteristic))
            .ToList();

        private static ReportedVariable? Find(List<ReportedVariable> reported, string component, string? componentInstance, string variable, string? variableInstance) =>
            reported.FirstOrDefault(r =>
                string.Equals(r.Component, component, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.ComponentInstance ?? "", componentInstance ?? "", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Variable, variable, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.VariableInstance ?? "", variableInstance ?? "", StringComparison.OrdinalIgnoreCase));

        private static int ItemsPerMessage(List<ReportedVariable> reported, string message = "SetVariables")
        {
            var value = Find(reported, "DeviceDataCtrlr", null, "ItemsPerMessage", message)
                ?.Attributes.FirstOrDefault(a => a.Type == AttributeEnumType.Actual)?.Value;
            return int.TryParse(value, out var items) && items > 0 ? items : DefaultItemsPerMessage;
        }

        private static void Annotate(ProvisioningPlanVariableDto variable, List<ReportedVariable> reported, bool haveReport)
        {
            var match = Find(reported, variable.ComponentName, variable.ComponentInstance, variable.VariableName, variable.VariableInstance);
            if (match == null)
            {
                variable.Support = haveReport ? "NotReported" : "Unknown";
                return;
            }

            var attributeType = Enum.TryParse<AttributeEnumType>(variable.AttributeType, true, out var t) ? t : AttributeEnumType.Actual;
            var attribute = match.Attributes.FirstOrDefault(a => (a.Type ?? AttributeEnumType.Actual) == attributeType);
            if (attribute == null)
            {
                variable.Support = "NotReported";
                return;
            }

            variable.CurrentValue = attribute.Value;
            variable.Mutability = attribute.Mutability?.ToString();
            variable.Support = attribute.Mutability == MutabilityEnumType.ReadOnly ? "ReadOnly" : "Writable";
            variable.DataType = match.Characteristic?.DataType.ToString();
            variable.Unit = match.Characteristic?.Unit;
            variable.ValuesList = match.Characteristic?.ValuesList;
            variable.MinLimit = match.Characteristic?.MinLimit;
            variable.MaxLimit = match.Characteristic?.MaxLimit;
        }

        // ---------------------------------------------------------------- SetVariables mapping

        private static IEnumerable<List<ProvisioningVariableDto>> Batches(List<ProvisioningVariableDto> variables, int size)
        {
            for (var i = 0; i < variables.Count; i += size)
                yield return variables.Skip(i).Take(size).ToList();
        }

        private static SetVariableDataType ToSetVariableData(ProvisioningVariableDto v) => new()
        {
            AttributeType = ParseAttribute(v.AttributeType),
            AttributeValue = v.Value,
            Component = ToComponent(v),
            Variable = ToVariable(v)
        };

        private static AttributeEnumType ParseAttribute(string? type) =>
            Enum.TryParse<AttributeEnumType>(type, true, out var t) ? t : AttributeEnumType.Actual;

        private static ComponentType ToComponent(ProvisioningVariableDto v) => new()
        {
            Name = v.ComponentName,
            Instance = string.IsNullOrWhiteSpace(v.ComponentInstance) ? null : v.ComponentInstance,
            Evse = v.EvseId.HasValue ? new EVSEType { Id = v.EvseId.Value, ConnectorId = v.ConnectorId } : null
        };

        private static VariableType ToVariable(ProvisioningVariableDto v) => new()
        {
            Name = v.VariableName,
            Instance = string.IsNullOrWhiteSpace(v.VariableInstance) ? null : v.VariableInstance
        };

        /// <summary>Writes values the charger confirmed into the saved device model (only variables it reported).</summary>
        private async Task StoreValues(int chargePointId, List<ProvisioningVariableResultDto> confirmed)
        {
            if (confirmed.Count == 0) return;

            var names = confirmed.Select(c => c.VariableName).Distinct().ToList();
            var items = await _db.OCPPConfigurationItems
                .Where(i => i.ChargePointID == chargePointId && names.Contains(i.OCPPConfigurationVariable!.Name))
                .Include(i => i.OCPPConfigurationComponent).ThenInclude(c => c!.OCPPConfigurationEVSE)
                .Include(i => i.OCPPConfigurationVariable)
                .Include(i => i.OCPPConfigurationVariableAttributes)
                .ToListAsync();

            foreach (var value in confirmed)
            {
                var evse = value.EvseId;
                var item = items.FirstOrDefault(i =>
                    string.Equals(i.OCPPConfigurationComponent?.Name, value.ComponentName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(i.OCPPConfigurationComponent?.Instance ?? "", value.ComponentInstance ?? "", StringComparison.OrdinalIgnoreCase) &&
                    i.OCPPConfigurationComponent?.OCPPConfigurationEVSE?.EVSEId == evse &&
                    (evse == null || i.OCPPConfigurationComponent?.OCPPConfigurationEVSE?.ConnectorId == value.ConnectorId) &&
                    string.Equals(i.OCPPConfigurationVariable?.Name, value.VariableName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(i.OCPPConfigurationVariable?.Instance ?? "", value.VariableInstance ?? "", StringComparison.OrdinalIgnoreCase));
                var type = ParseAttribute(value.AttributeType);
                var attribute = item?.OCPPConfigurationVariableAttributes?.FirstOrDefault(a => (a.Type ?? AttributeEnumType.Actual) == type);
                if (attribute == null) continue;
                attribute.Value = value.Value;
                attribute.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>Pairs each sent variable with its result: by component/variable identity, else by position.</summary>
        private static IEnumerable<ProvisioningVariableResultDto> MatchResults(List<ProvisioningVariableDto> sent, List<SetVariableResultType> results)
        {
            var unused = results.ToList();
            for (var i = 0; i < sent.Count; i++)
            {
                var v = sent[i];
                var match = unused.FirstOrDefault(r =>
                    string.Equals(r.component?.Name, v.ComponentName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.component?.Instance ?? "", v.ComponentInstance ?? "", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.variable?.Name, v.VariableName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.variable?.Instance ?? "", v.VariableInstance ?? "", StringComparison.OrdinalIgnoreCase))
                    ?? (i < results.Count && unused.Contains(results[i]) ? results[i] : null);

                if (match == null)
                {
                    yield return ToResult(v, "NoResponse", "The charger returned no result for this variable.");
                    continue;
                }

                unused.Remove(match);
                yield return ToResult(v, match.attributeStatus?.ToString() ?? "Unknown", match.attributeStatusInfo?.ReasonCode);
            }
        }

        private static ProvisioningVariableResultDto ToResult(ProvisioningVariableDto v, string status, string? reason) => new()
        {
            GroupName = v.GroupName,
            ComponentName = v.ComponentName,
            ComponentInstance = v.ComponentInstance,
            EvseId = v.EvseId,
            ConnectorId = v.ConnectorId,
            VariableName = v.VariableName,
            VariableInstance = v.VariableInstance,
            AttributeType = v.AttributeType,
            Value = v.Value,
            Description = v.Description,
            Status = status,
            StatusReason = reason
        };
    }
}
