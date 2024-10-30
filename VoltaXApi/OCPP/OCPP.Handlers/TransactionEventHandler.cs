using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class TransactionEventHandler : IOCPPRequestHandler
  {
      public string HandleTransactionEvent(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string? errorCode = null;
            TransactionEventResponse transactionEventResponse = new TransactionEventResponse();
            transactionEventResponse.CustomData = new CustomDataType();
            transactionEventResponse.CustomData.VendorId = VendorId;
            transactionEventResponse.IdTokenInfo = new IdTokenInfoType();

            int connectorId = 0;

            try
            {
                Console.WriteLine("TransactionEvent => Processing transactionEvent request...");
                TransactionEventRequest transactionEventRequest = JsonConvert.DeserializeObject<TransactionEventRequest>(msgIn.JsonPayload);
                Console.WriteLine("TransactionEvent => Message deserialized");

                string idTag = CleanChargeTagId(transactionEventRequest.IdToken.IdToken, Logger);
                Console.WriteLine("this is the idTag : " + idTag);
                connectorId = (transactionEventRequest.EVSE != null) ? (int) transactionEventRequest.EVSE.ConnectorId : 0;


                //  Extract meter values with correct scale
                double currentChargeKW = -1;
                double meterKWH = -1;
                DateTimeOffset? meterTime = null;
                double stateOfCharge = -1;
                GetMeterValues(transactionEventRequest.MeterValues, out meterKWH, out currentChargeKW, out stateOfCharge, out meterTime);


                if (transactionEventRequest.EventType == TransactionEventEnumType.Started)
                {
                    try
                    {
                        #region Start Transaction
                        var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
                        optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                        using (VoltaXApiDbContext dbContext = new VoltaXApiDbContext(optionsBuilder.Options))
                        {
                            if (string.IsNullOrWhiteSpace(idTag))
                            {
                                // no RFID-Tag => accept request
                                transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                                Console.WriteLine("StartTransaction => no charge tag => accepted");
                            }
                            else
                            {
                                ChargeTag? ct = dbContext.ChargeTags.Where(u => u.TagID == idTag).FirstOrDefault();
                                if (ct != null)
                                {
                                    if (ct.Blocked.HasValue && ct.Blocked.Value)
                                    {
                                        Console.WriteLine("StartTransaction => Tag '{0}' blocked)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                                    }
                                    else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                                    {
                                        Console.WriteLine("StartTransaction => Tag '{0}' expired)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                                    }
                                    else
                                    {
                                        Console.WriteLine("StartTransaction => Tag '{0}' accepted)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("StartTransaction => Tag '{0}' unknown)", idTag);
                                    transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Unknown;
                                }
                            }

                            if (transactionEventResponse.IdTokenInfo.Status == AuthorizationStatusEnumType.Accepted)
                            {
                                try
                                {
                                    Console.WriteLine("StartTransaction => Meter='{0}' (kWh)", meterKWH);
                                    Console.WriteLine(transactionEventRequest.TransactionInfo.TransactionId);
                                    Transaction transaction = new Transaction();
                                    transaction.Uid = transactionEventRequest.TransactionInfo.TransactionId;
                                    transaction.ChargePointID = ChargePointStatus.Id;
                                    transaction.ConnectorID = connectorId;
                                    transaction.StartTagId = idTag;
                                    transaction.StartTime =  DateTime.Parse(transactionEventRequest.Timestamp);
                                    transaction.MeterStart  = meterKWH;
                                    transaction.StartResult = transactionEventRequest.TriggerReason.ToString();
                                    dbContext.Add<Transaction>(transaction);

                                    dbContext.SaveChanges();
                                }
                                catch (Exception exp)
                                {
                                    Console.WriteLine( "StartTransaction => Exception writing transaction: chargepoint={0} / tag={1}", ChargePointStatus?.Id, idTag);
                                    Console.WriteLine(exp.Message); 
                                    Console.WriteLine(exp.InnerException);
                                    Console.WriteLine(exp.StackTrace);
                                    errorCode = ErrorCodes.InternalError;
                                }
                            }
                        }
                        #endregion
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine( "StartTransaction => Exception: {0}", exp.Message);
                        Console.WriteLine(exp.StackTrace);
                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                    }
                }
                else if (transactionEventRequest.EventType == TransactionEventEnumType.Updated)
                {
                    try
                    {
                        #region Update Transaction
                        var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
                        optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                        using (VoltaXApiDbContext dbContext = new VoltaXApiDbContext(optionsBuilder.Options))
                        {
                            Transaction? transaction = dbContext.Transactions
                                .Where(t => t.Uid == transactionEventRequest.TransactionInfo.TransactionId)
                                .OrderByDescending(t => t.ID)
                                .FirstOrDefault();
                            if (transaction == null ||
                                transaction.ChargePointID != ChargePointStatus.Id ||
                                transaction.StopTime.HasValue)
                            {
                                // unknown transaction id or already stopped transaction
                                // => find latest transaction for the charge point and check if its open
                                Console.WriteLine("UpdateTransaction => Unknown or closed transaction uid={0}", transactionEventRequest.TransactionInfo?.TransactionId);
                                // find latest transaction for this charge point
                                transaction = dbContext.Transactions
                                    .Where(t => t.ChargePointID == ChargePointStatus.Id && t.ConnectorID == connectorId)
                                    .OrderByDescending(t => t.ID)
                                    .FirstOrDefault();

                                if (transaction != null)
                                {
                                    Console.WriteLine("UpdateTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'", transaction.ID, transaction.StartTime.ToString("O"), transaction?.StopTime?.ToString("O"));
                                    if (transaction.StopTime.HasValue)
                                    {
                                        Console.WriteLine("UpdateTransaction => Last transaction (id={0}) is already closed ", transaction.ID);
                                        transaction = null;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("UpdateTransaction => Found no transaction for charge point '{0}' and connectorId '{1}'", ChargePointStatus.Id, connectorId);
                                }
                            }

                            if (transaction != null)
                            {
                                // write current meter value in "stop" value
                                if (meterKWH >= 0)
                                {
                                    Console.WriteLine("UpdateTransaction => Meter='{0}' (kWh)", meterKWH);
                                    transaction.MeterStop = meterKWH;
                                    dbContext.SaveChanges();
                                }
                            }
                            else
                            {
                                Console.WriteLine("UpdateTransaction => Unknown transaction: uid='{0}' / chargepoint='{1}' / tag={2}", transactionEventRequest.TransactionInfo?.TransactionId, ChargePointStatus?.Id, idTag);
                                WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, string.Format("UnknownTransaction:UID={0}/Meter={1}", transactionEventRequest.TransactionInfo?.TransactionId, GetMeterValue(transactionEventRequest.MeterValues)), errorCode);
                                errorCode = ErrorCodes.PropertyConstraintViolation;
                            }
                        }
                        #endregion
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine( "UpdateTransaction => Exception: {0}", exp.Message);
                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                    }
                }
                else if (transactionEventRequest.EventType == TransactionEventEnumType.Ended)
                {
                    try
                    {
                        #region End Transaction
                        var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
                        optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                        using (VoltaXApiDbContext dbContext = new VoltaXApiDbContext(optionsBuilder.Options))
                        {
                            ChargeTag? ct = null;

                            if (string.IsNullOrWhiteSpace(idTag))
                            {
                                // no RFID-Tag => accept request
                                transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                                Console.WriteLine("EndTransaction => no charge tag => accepted");
                            }
                            else
                            {
                                ct = dbContext.ChargeTags.Where(c => c.TagID == idTag).FirstOrDefault();
                                if (ct != null)
                                {
                                    if (ct.Blocked.HasValue && ct.Blocked.Value)
                                    {
                                        Console.WriteLine("EndTransaction => Tag '{0}' blocked)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                                    }
                                    else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                                    {
                                        Console.WriteLine("EndTransaction => Tag '{0}' expired)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                                    }
                                    else
                                    {
                                        Console.WriteLine("EndTransaction => Tag '{0}' accepted)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("EndTransaction => Tag '{0}' unknown)", idTag);
                                    transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Unknown;
                                }
                            }

                            Transaction? transaction = dbContext.Transactions
                                .Where(t => t.Uid == transactionEventRequest.TransactionInfo.TransactionId)
                                .OrderByDescending(t => t.ID)
                                .FirstOrDefault();
                            if (transaction == null ||
                                transaction.ChargePointID != ChargePointStatus.Id ||
                                transaction.StopTime.HasValue)
                            {
                                // unknown transaction id or already stopped transaction
                                // => find latest transaction for the charge point and check if its open
                                Console.WriteLine("EndTransaction => Unknown or closed transaction uid={0}", transactionEventRequest.TransactionInfo?.TransactionId);
                                // find latest transaction for this charge point
                                transaction = dbContext.Transactions
                                    .Where(t => t.ChargePointID == ChargePointStatus.Id && t.ConnectorID == connectorId)
                                    .OrderByDescending(t => t.ID)
                                    .FirstOrDefault();

                                if (transaction != null)
                                {
                                    Console.WriteLine("EndTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'", transaction.ID, transaction.StartTime.ToString("O"), transaction?.StopTime?.ToString("O"));
                                    if (transaction.StopTime.HasValue)
                                    {
                                        Console.WriteLine("EndTransaction => Last transaction (id={0}) is already closed ", transaction.ID);
                                        transaction = null;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("EndTransaction => Found no transaction for charge point '{0}' and connectorId '{1}'", ChargePointStatus.Id, connectorId);
                                }
                            }

                            if (transaction != null)
                            {
                                // check current tag against start tag
                                bool valid = true;
                                if (!string.Equals(transaction.StartTagId, idTag, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // tags are different => same group?
                                    ChargeTag? startTag = dbContext.ChargeTags.Where(c => c.TagID == transaction.StartTagId).FirstOrDefault();
                                    if (startTag != null)
                                    {
                                        if (!string.Equals(startTag.ParentTagId, ct?.ParentTagId, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            Console.WriteLine("EndTransaction => Start-Tag ('{0}') and End-Tag ('{1}') do not match: Invalid!", transaction.StartTagId, ct?.ID);
                                            transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                                            valid = false;
                                        }
                                        else
                                        {
                                            Console.WriteLine("EndTransaction => Different charge tags but matching group ('{0}')", ct?.ParentTagId);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("EndTransaction => Start-Tag not found: '{0}'", transaction.StartTagId);
                                        // assume "valid" and allow to end the transaction
                                    }
                                }

                                if (valid)
                                {
                                    // write current meter value in "stop" value
                                    Console.WriteLine("EndTransaction => Meter='{0}' (kWh)", meterKWH);

                                    transaction.StopTime = DateTime.Parse(transactionEventRequest.Timestamp);
                                    transaction.MeterStop = meterKWH;
                                    transaction.StopTagId = idTag;
                                    transaction.StopReason = transactionEventRequest.TriggerReason.ToString();
                                    dbContext.SaveChanges();

                                    // Update connecter status to available

                                }
                            }
                            else
                            {
                                Console.WriteLine("EndTransaction => Unknown transaction: uid='{0}' / chargepoint='{1}' / tag={2}", transactionEventRequest.TransactionInfo?.TransactionId, ChargePointStatus?.Id, idTag);
                                WriteMessageLog(ChargePointStatus?.Id, connectorId, msgIn.Action, string.Format("UnknownTransaction:UID={0}/Meter={1}", transactionEventRequest.TransactionInfo?.TransactionId, GetMeterValue(transactionEventRequest.MeterValues)), errorCode);
                                errorCode = ErrorCodes.PropertyConstraintViolation;
                            }
                        }
                        #endregion
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine( "EndTransaction => Exception: {0}", exp.Message);
                        Console.WriteLine(exp.StackTrace);
                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                    }
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(transactionEventResponse);
                Console.WriteLine("TransactionEvent => Response serialized");
            }
            catch (Exception exp)
            {
                Console.WriteLine( "TransactionEvent => Exception: {0}", exp.Message);
                Console.WriteLine(exp.StackTrace);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, connectorId, msgIn.Action, transactionEventResponse.IdTokenInfo.Status.ToString(), errorCode);
            return errorCode;
        }


        /// <summary>
        /// Extract main meter value from collection
        /// </summary>
        private double GetMeterValue(ICollection<MeterValueType> meterValues)
        {
            double currentChargeKW = -1;
            double meterKWH = -1;
            DateTimeOffset? meterTime = null;
            double stateOfCharge = -1;
            GetMeterValues(meterValues, out meterKWH, out currentChargeKW, out stateOfCharge, out meterTime);

            return meterKWH;
        }

        /// <summary>
        /// Extract different meter values from collection
        /// </summary>
        private void GetMeterValues(ICollection<MeterValueType> meterValues, out double meterKWH, out double currentChargeKW, out double stateOfCharge, out DateTimeOffset? meterTime)
        {
            currentChargeKW = -1;
            meterKWH = -1;
            meterTime = null;
            stateOfCharge = -1;

            foreach (MeterValueType meterValue in meterValues)
            {
                
                foreach (SampledValueType sampleValue in meterValue.SampledValue)
                {
                    Console.WriteLine("this is the first metervaluetype : ");
                    Console.WriteLine(sampleValue.Value);
                    Console.WriteLine(sampleValue.Measurand);
                    Console.WriteLine("GetMeterValues => Context={0} / SignedMeterValue={1} / Value={2} / Unit={3} / Location={4} / Measurand={5} / Phase={6}",
                        sampleValue.Context, sampleValue.SignedMeterValue, sampleValue.Value, sampleValue.UnitOfMeasure, sampleValue.Location, sampleValue.Measurand, sampleValue.Phase);

                    if (sampleValue.Measurand == MeasurandEnumType.Power_Active_Import)
                    {
                        // current charging power
                        currentChargeKW = sampleValue.Value;
                        if (sampleValue.UnitOfMeasure?.Unit == "W" ||
                            sampleValue.UnitOfMeasure?.Unit == "VA" ||
                            sampleValue.UnitOfMeasure?.Unit == "var" ||
                            sampleValue.UnitOfMeasure?.Unit == null ||
                            sampleValue.UnitOfMeasure == null)
                        {
                            Console.WriteLine("GetMeterValues => Charging '{0:0.0}' W", currentChargeKW);
                            // convert W => kW
                            currentChargeKW = currentChargeKW / 1000;
                        }
                        else if (sampleValue.UnitOfMeasure?.Unit == "KW" ||
                                sampleValue.UnitOfMeasure?.Unit == "kVA" ||
                                sampleValue.UnitOfMeasure?.Unit == "kvar")
                        {
                            // already kW => OK
                            Console.WriteLine("GetMeterValues => Charging '{0:0.0}' kW", currentChargeKW);
                        }
                        else
                        {
                            Console.WriteLine("GetMeterValues => Charging: unexpected unit: '{0}' (Value={1})", sampleValue.UnitOfMeasure?.Unit, sampleValue.Value);
                        }
                    }
                    else if (sampleValue.Measurand == MeasurandEnumType.Energy_Active_Import_Register)  
                    {
                        // charged amount of energy
                        meterKWH = sampleValue.Value;
                        if (sampleValue.UnitOfMeasure?.Unit == "Wh" ||
                            sampleValue.UnitOfMeasure?.Unit == "VAh" ||
                            sampleValue.UnitOfMeasure?.Unit == "varh" ||
                            (sampleValue.UnitOfMeasure == null || sampleValue.UnitOfMeasure.Unit == null))
                        {
                            // Multiplying this by the meter value
                            Console.WriteLine("GetMeterValues => Value: '{0:0.0}' Wh", meterKWH);
                            if(sampleValue.UnitOfMeasure?.Multiplier != null && sampleValue.UnitOfMeasure?.Multiplier>0)
                            meterKWH = meterKWH * Math.Pow(10,(double) sampleValue.UnitOfMeasure?.Multiplier);

                            // convert Wh => kWh
                            meterKWH = meterKWH / 1000;
                        }
                        else if (sampleValue.UnitOfMeasure?.Unit == "kWh" ||
                                sampleValue.UnitOfMeasure?.Unit == "kVAh" ||
                                sampleValue.UnitOfMeasure?.Unit == "kvarh")
                        {
                            // already kWh => OK
                            Console.WriteLine("GetMeterValues => Value: '{0:0.0}' kWh", meterKWH);
                        }
                        else
                        {
                            Console.WriteLine("GetMeterValues => Value: unexpected unit: '{0}' (Value={1})", sampleValue.UnitOfMeasure?.Unit, sampleValue.Value);
                        }
                        meterTime = meterValue.Timestamp;
                    }
                    else if (sampleValue.Measurand == MeasurandEnumType.SoC)
                    {
                        // state of charge (battery status)
                        stateOfCharge = sampleValue.Value;
                        Console.WriteLine("GetMeterValues => SoC: '{0:0.0}'%", stateOfCharge);
                    }
                }
            }
        }
  }
}