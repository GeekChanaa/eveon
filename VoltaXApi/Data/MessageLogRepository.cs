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


namespace VoltaXApi.Data
{
    public class MessageLogRepository : Repository<MessageLog>,IMessageLogRepository
    {
        private readonly IMapper _mapper;
        public MessageLogRepository(VoltaXApiDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }
        
        public bool WriteMessageLog(string chargePointId, int? connectorId, string message, string result, string errorCode)
        {
            try
            {
                int dbMessageLog = 2;
                if (dbMessageLog > 0 && !string.IsNullOrWhiteSpace(chargePointId))
                {
                    bool doLog = (dbMessageLog > 1 ||
                                    (message != "BootNotification" &&
                                     message != "Heartbeat" &&
                                     message != "DataTransfer" &&
                                     message != "StatusNotification"));

                    if (doLog)
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<VoltaXApiDbContext>();
                        MessageLog msgLog = new MessageLog();
                        msgLog.ChargePointId = chargePointId;
                        msgLog.ConnectorId = connectorId;
                        msgLog.LogTime = DateTime.UtcNow;
                        msgLog.Message = message;
                        msgLog.Result = result;
                        msgLog.ErrorCode = errorCode;
                        _context.MessageLogs.Add(msgLog);
                        _context.SaveChanges();
                        return true;
                    }
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

