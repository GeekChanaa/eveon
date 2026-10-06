using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using System.Net.WebSockets;
using VoltaXApi.OCPP.Services;
using VoltaXApi.Services;
using VoltaxApi.Dtos;
using System.Text.Json;
using VoltaXApi.OCPP.Messages;


namespace VoltaXApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChargePointController : GenericController<ChargePoint>
    {
        private readonly IChargePointRepository _repository;
        private readonly WebSocketManagerService _wsService;
        private readonly IQRCodeService _qrCodeService;

        public ChargePointController(
            IChargePointRepository repository,
            WebSocketManagerService wsService,
            IQRCodeService qrCodeService) : base(repository)
        {
            _repository = repository;
            _wsService = wsService;
            _qrCodeService = qrCodeService;
        }

        // Get ChargePoint Connectors
        [HttpGet("GetChargePointConnectors")]
        public async Task<ActionResult<List<Connector>>> GetChargePointConnectors([FromQuery] int chargePointID)
        {
            return await this._repository.GetChargePointConnectors(chargePointID);
        }

        // The entity-bound generic actions are disabled: create and update go through validated DTOs.
        [NonAction]
        public override Task<IActionResult> Create(ChargePoint chargePoint) => Task.FromResult<IActionResult>(BadRequest());

        [NonAction]
        public override Task<IActionResult> Update(int id, JsonElement entityToUpdate) => Task.FromResult<IActionResult>(BadRequest());

        [HttpPost]
        public async Task<IActionResult> CreateChargePoint([FromBody] ChargePointCreateRequestDto request)
        {
            var chargePoint = new ChargePoint
            {
                ChargingStationID = request.ChargingStationID,
                SerialNumber = request.SerialNumber.Trim(),
                ChargePointModelID = request.ChargePointModelID,
                ChargePointBrandID = request.ChargePointBrandID,
                Status = request.Status,
                Category = request.Category,
                Comment = request.Comment,
                Username = request.Username,
                ClientCertThumb = request.ClientCertThumb ?? "",
                ShowOnMap = request.ShowOnMap ?? true,
                HasChargeCable = request.HasChargeCable ?? true,
                Connectors = request.Connectors?.Select(c => new Connector
                {
                    ConnectorID = c.ConnectorID,
                    EvseID = c.EvseID,
                    ConnectorType = c.ConnectorType ?? c.Type ?? ConnectorEnumType.cType2,
                    Power = c.Power ?? 0,
                    MaxPower = c.MaxPower ?? 100,
                    PricePerKWh = c.PricePerKWh ?? 0,
                    PricePerMinute = c.PricePerMinute ?? 0,
                    PricePerIdleMinute = c.PricePerIdleMinute ?? 0,
                    CostPerKwh = c.CostPerKwh ?? 0,
                    FlatFee = c.FlatFee ?? 0
                }).ToList()
            };

            await _repository.AddAsync(chargePoint);
            return Ok(new { id = chargePoint.ID });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChargePoint(int id, [FromBody] ChargePointUpdateRequestDto request)
        {
            var chargePoint = await _repository.GetByIdAsync(id);
            if (chargePoint == null) return NotFound();

            if (request.SerialNumber != null) chargePoint.SerialNumber = request.SerialNumber.Trim();
            if (request.ChargePointModelID != null) chargePoint.ChargePointModelID = request.ChargePointModelID;
            if (request.ChargePointBrandID != null) chargePoint.ChargePointBrandID = request.ChargePointBrandID;
            if (request.Status != null) chargePoint.Status = request.Status.Value;
            if (request.Category != null) chargePoint.Category = request.Category.Value;
            if (request.Comment != null) chargePoint.Comment = request.Comment;
            if (request.ClientCertThumb != null) chargePoint.ClientCertThumb = request.ClientCertThumb;
            if (request.ShowOnMap != null) chargePoint.ShowOnMap = request.ShowOnMap;
            if (request.HasChargeCable != null) chargePoint.HasChargeCable = request.HasChargeCable;

            await _repository.Update(chargePoint);
            return NoContent();
        }

        // Charging Stations of partner
        [HttpGet("GetPartnerChargePoints/{partnerID}")]
        public async Task<ActionResult<List<ChargingStation>>> GetPartnerChargePoints(int partnerID, [FromQuery] GlobalParams globalParams)
        {
            var chargingStations = await PagedList<ChargePoint>.CreateAsync((_repository.GetAllAsync(globalParams)).Where(u => u.ChargingStation.PartnerID == partnerID), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargingStations.CurrentPage, chargingStations.PageSize, chargingStations.TotalCount, chargingStations.TotalPages);
            return Ok(chargingStations);
        }

        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id)
        {
            var entity = await this._repository.GetChargePointByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpGet("GetChargingStationChargePoints/{id}")]
        public async Task<IActionResult> GetChargingStationChargePoints(int id)
        {
            var entity = await this._repository.GetChargingStationChargePoints(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpGet("IsChargePointIDUnique/{chargePointID}")]
        public async Task<ActionResult<bool>> IsChargePointIDUnique(string chargePointID)
        {
            return await this._repository.IsChargePointIDUnique(chargePointID);
        }

        [HttpGet("IsChargePointSerialNumberUnique/{chargePointSerialNumber}")]
        public async Task<ActionResult<bool>> IsChargePointSerialNumberUnique(string chargePointSerialNumber)
        {
            return await this._repository.IsChargePointSerialNumberUnique(chargePointSerialNumber);
        }

        [HttpGet("GetChargePointByID/{chargePointID}")]
        public async Task<IActionResult> GetChargePointByID(int chargePointID)
        {
            var helper = new ChargePointIncludableHelper { };
            return Ok(await this._repository.GetChargePointByID(chargePointID, helper));
        }

        [HttpGet("GetAllChargePoints")]
        public async Task<List<ChargePointCRListDto>> GetAllChargePoints([FromQuery] GlobalParams globalParams, [FromServices] VoltaXApi.OCPP.Core.IOcppCommandSender commandSender)
        {
            var chargePoints = this._repository.GetAllChargePoints(globalParams);
            var chargePointsList = await PagedList<ChargePointCRListDto>.CreateAsync(chargePoints,globalParams.PageNumber, globalParams.PageSize);
            foreach (var chargePoint in chargePointsList)
            {
                chargePoint.IsOnline = _wsService.GetWebSocket(chargePoint.ChargePointId)?.State == WebSocketState.Open;
                chargePoint.ProtocolVersion = commandSender.GetProtocolVersion(chargePoint.ChargePointId);
            }
            Response.AddPagination(chargePointsList.CurrentPage, chargePointsList.PageSize, chargePointsList.TotalCount, chargePointsList.TotalPages);
            return chargePointsList;
        }

        [HttpGet("GetChargePointsIds")]
        public async Task<IActionResult> GetChargePointsIds()
        {
            return Ok(await this._repository.GetChargePointsIds());
        }

        [HttpPost("{chargePointId}/sendMessage")]
        public async Task<IActionResult> SendMessageToChargePoint(string chargePointId, [FromBody] string message)
        {
            var webSocket = _wsService.GetWebSocket(chargePointId);

            if (webSocket == null || webSocket.State != WebSocketState.Open)
            {
                return NotFound("WebSocket connection for this charge point is not available.");
            }
            
            await _wsService.SendMessageAsync(chargePointId, message);

            return Ok("Message sent successfully.");
        }
        
        [HttpPut("SetShowOnMap/{chargePointID}")]
        public async Task<IActionResult> SetShowOnMap(int chargePointID, [FromBody] bool val)
        {
            await this._repository.SetShowOnMap(chargePointID, val);
            return StatusCode(200);
        }

        [HttpPut("SetHasChargeCable/{chargePointID}")]
        public async Task<IActionResult> SetHasChargeCable(int chargePointID, [FromBody] bool val)
        {
            await this._repository.SetHasChargeCable(chargePointID, val);
            return StatusCode(200);
        }

        /// <summary>
        /// Generates a QR code image for a specific charge point
        /// </summary>
        /// <param name="chargePointID">The charge point ID</param>
        /// <returns>PNG image of the QR code</returns>
        [HttpGet("GenerateQrCodeForChargePoint/{chargePointID}")]
        public async Task<IActionResult> GenerateQrCodeForChargePoint(int chargePointID)
        {
            var chargePoint = await _repository.GetByIdAsync(chargePointID);
            
            if (chargePoint == null)
            {
                return NotFound(new { message = "Charge point not found" });
            }

            if (string.IsNullOrEmpty(chargePoint.QrValue))
            {
                return BadRequest(new { message = "Charge point does not have a QR value" });
            }

            // Failures reach GlobalExceptionFilter: logged in full, generic message to the client.
            var qrCodeBytes = _qrCodeService.GenerateQr(chargePoint.QrValue);
            return File(qrCodeBytes, "image/png", $"chargepoint_{chargePointID}_qr.png");
        }

        /// <summary>
        /// Replaces the QR value of a charge point. Previously printed QR codes stop working.
        /// </summary>
        [HttpPost("RegenerateQrCode/{chargePointID}")]
        public async Task<IActionResult> RegenerateQrCode(int chargePointID)
        {
            if (!await _repository.RegenerateQrValue(chargePointID))
            {
                return NotFound(new { message = "Charge point not found" });
            }
            return Ok();
        }

        /// <summary>
        /// Replaces the QR values of all charge points. Previously printed QR codes stop working.
        /// </summary>
        [HttpPost("RegenerateAllQrCodes")]
        public async Task<IActionResult> RegenerateAllQrCodes()
        {
            var count = await _repository.RegenerateAllQrValues();
            return Ok(new { count });
        }

        /// <summary>
        /// Sets (or generates) the OCPP Basic auth password. The plain value is only returned in this response.
        /// </summary>
        [HttpPost("SetPassword/{chargePointID}")]
        public async Task<ActionResult<ChargePointPasswordResultDto>> SetPassword(int chargePointID, [FromBody] ChargePointPasswordSetDto dto)
        {
            var password = dto.Generate ? ChargePointPasswordHasher.Generate() : dto.Password;
            if (!ChargePointPasswordHasher.IsValidPassword(password))
            {
                return BadRequest(new { message = $"Password must be {ChargePointPasswordHasher.MinLength}-{ChargePointPasswordHasher.MaxLength} printable ASCII characters." });
            }

            var chargePoint = await _repository.GetByIdAsync(chargePointID);
            if (chargePoint == null || chargePoint.IsDeleted || !await _repository.SetPasswordHash(chargePointID, ChargePointPasswordHasher.Hash(password!)))
            {
                return NotFound(new { message = "Charge point not found" });
            }

            Response.Headers["Cache-Control"] = "no-store";
            return Ok(new ChargePointPasswordResultDto { Username = chargePoint.ChargePointId, Password = password! });
        }

        [HttpGet("GetChargePointByQrCode/{qrCode}")]
        public async Task<ActionResult<ChargePointDetailsForMobileDto>> GetChargePointByQrCode(string qrCode) 
        {
            var chargePoint = await this._repository.GetChargePointByQrCode(qrCode);
            if (chargePoint == null)
            {
                return NotFound();
            }

            return Ok(chargePoint);
        }

    }
}