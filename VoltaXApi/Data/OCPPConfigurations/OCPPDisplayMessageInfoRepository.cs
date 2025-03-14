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
using AutoMapper.QueryableExtensions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Data
{
    public class OCPPDisplayMessageInfoRepository : Repository<OCPPDisplayMessageInfo>,IOCPPDisplayMessageInfoRepository
    {
        private readonly IMapper _mapper;
        private readonly ILogger<OCPPDisplayMessageInfoRepository> _logger;
        private readonly IOCPPConfigurationComponentRepository _componentRepository;
        public OCPPDisplayMessageInfoRepository(
            VoltaXApiDbContext context,
            IOCPPConfigurationComponentRepository componentRepository,
            ILogger<OCPPDisplayMessageInfoRepository> logger,
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
            _logger = logger;
            _componentRepository = componentRepository;
        }

        public async Task<List<OCPPDisplayMessageInfo>> GetMessagesByChargePointIdAsync(string chargePointId)
        {
            _logger.LogInformation("Getting all messages for charge point: {ChargePointId}", chargePointId);
            return await _context.OCPPDisplayMessageInfos
                .Where(m => m.ChargePointID == chargePointId)
                .ToListAsync();
        }

        public async Task<OCPPDisplayMessageInfo> GetMessageByIdAsync(int id, string chargePointID)
        {
            _logger.LogInformation("Getting message with ID: {MessageId} for charge point: {chargePointID}", id, chargePointID);
            return await _context.OCPPDisplayMessageInfos
                .FirstOrDefaultAsync(m => m.ID == id && m.ChargePointID == chargePointID);
        }

        public async Task<List<OCPPDisplayMessageInfo>> GetActiveMessagesAsync(string chargePointID)
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation("Getting active messages for charge point: {ChargePointId}", chargePointID);
            
            return await _context.OCPPDisplayMessageInfos
                .Where(m => m.ChargePointID == chargePointID)
                .Where(m => (m.StartDateTime == null || m.StartDateTime <= now) && 
                            (m.EndDateTime == null || m.EndDateTime >= now))
                .ToListAsync();
        }

        public async Task AddMessageAsync(MessageInfoType message, string chargePointId)
        {
             if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrEmpty(chargePointId))
                throw new ArgumentException("ChargePointId cannot be null or empty", nameof(chargePointId));

            _logger.LogInformation("Adding new message with ID: {MessageId} for charge point: {ChargePointId}", 
                message.Id, chargePointId);

            //Find or create component
            var component = await _componentRepository.FindOrCreateComponent(message.Display);

            // Create your database entity from the message
            var messageEntity = new OCPPDisplayMessageInfo
            {
                ChargePointID = chargePointId,
                DisplayMessageId = message.Id,
                DisplayID = component.ID,
                Priority = message.Priority,
                State = message.State,
                StartDateTime = message.StartDateTime,
                EndDateTime = message.EndDateTime,
                TransactionId = message.TransactionId,
                Message = new OCPPDisplayMessageContent{
                    Format = message.Message.Format,
                    Language = message.Message.Language,
                    Content = message.Message.Content,
                }
            };

            await _context.OCPPDisplayMessageInfos.AddAsync(messageEntity);
            await _context.SaveChangesAsync();
        }

        public async Task AddMessagesAsync(IEnumerable<MessageInfoType> messages, string chargePointId)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            var messageList = messages.ToList();
            if (!messageList.Any())
                return;

            _logger.LogInformation("Adding {Count} messages for charge point: {ChargePointId}", messageList.Count, chargePointId);
            
            // Set ChargePointId if not already set
            foreach (var message in messageList)
            {
                await this.AddMessageAsync(message,chargePointId);
            }
        }

        public async Task UpdateMessageAsync(OCPPDisplayMessageInfo message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            _logger.LogInformation("Updating message with ID: {MessageId} for charge point: {ChargePointId}", 
                message.ID, message.ChargePointID);
            
            _context.Entry(message).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMessageAsync(int id, string chargePointId)
        {
            var message = await _context.OCPPDisplayMessageInfos
                .FirstOrDefaultAsync(m => m.ID == id && m.ChargePointID == chargePointId);
            
            if (message != null)
            {
                _logger.LogInformation("Deleting message with ID: {MessageId} for charge point: {ChargePointId}", 
                    id, chargePointId);
                
                _context.OCPPDisplayMessageInfos.Remove(message);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Message with ID: {MessageId} for charge point: {ChargePointId} not found for deletion", 
                    id, chargePointId);
            }
        }

        public async Task DeleteExpiredMessagesAsync()
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation("Deleting expired messages at {Timestamp}", now);
            
            var expiredMessages = await _context.OCPPDisplayMessageInfos
                .Where(m => m.EndDateTime != null && m.EndDateTime < now)
                .ToListAsync();
            
            if (expiredMessages.Any())
            {
                _logger.LogInformation("Found {Count} expired messages to delete", expiredMessages.Count);
                _context.OCPPDisplayMessageInfos.RemoveRange(expiredMessages);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation("No expired messages found to delete");
            }
        }

    }
}

