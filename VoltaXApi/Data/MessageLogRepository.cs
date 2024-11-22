using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using AutoMapper;
using System.Configuration;
using VoltaXApi.OCPP.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace VoltaXApi.Data
{
    public class MessageLogRepository : Repository<MessageLog>,IMessageLogRepository
    {
        private readonly IMapper _mapper;
        public MessageLogRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }
        
        public async Task<bool> SaveLogMessage(string chargePointId, 
                                                int? connectorId, 
                                                string message, 
                                                string result, 
                                                string errorCode, 
                                                OCPPMessage sent, 
                                                OCPPMessage received)
        {
            try
            {
                var ocppArrayMessage = new object[]
                {
                    JRaw.Parse(received.MessageType),
                    received.UniqueId,   
                    received.Action,     
                    received.JsonPayload != null ? JRaw.Parse(received.JsonPayload)  : ""
                };

                if(received.MessageType == "3")
                {
                    ocppArrayMessage = new object[]
                    {
                        JRaw.Parse(received.MessageType),
                        received.UniqueId,   
                        received.JsonPayload != null ? JRaw.Parse(received.JsonPayload)  : ""
                    };
                }

                var settings = new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> { new StringEnumConverter() }
                };
                Console.WriteLine("this is the msgout jsonpayload : ");
                Console.WriteLine(received.JsonPayload);
                
                string serializedMessage = JsonConvert.SerializeObject(ocppArrayMessage,settings);
                Console.WriteLine("this is the ocppArrayMessage : ");
                Console.WriteLine(serializedMessage);
                received.RawMessage = serializedMessage;
                if (!string.IsNullOrWhiteSpace(chargePointId))
                {
                    MessageLog msgLog = new MessageLog();
                    msgLog.ChargePointId = chargePointId;
                    msgLog.ConnectorId = connectorId;
                    msgLog.LogTime = DateTime.UtcNow;
                    msgLog.Message = message;
                    msgLog.Result = result;
                    msgLog.ErrorCode = errorCode;
                    msgLog.ContentSent = sent.RawMessage;
                    msgLog.ContentReceived = received.RawMessage;
                    await _context.MessageLogs.AddAsync(msgLog);
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                Console.WriteLine(exp.StackTrace);
            }
            return false;
        }

        

    }
}

