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
        
        public async Task<bool> SaveLogMessage(string chargePointId, int? connectorId, string message, string result, string errorCode)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(chargePointId))
                {
                    MessageLog msgLog = new MessageLog();
                    msgLog.ChargePointId = chargePointId;
                    msgLog.ConnectorId = connectorId;
                    msgLog.LogTime = DateTime.UtcNow;
                    msgLog.Message = message;
                    msgLog.Result = result;
                    msgLog.ErrorCode = errorCode;
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

