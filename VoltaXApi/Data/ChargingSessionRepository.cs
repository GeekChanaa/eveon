using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using VoltaXApi.Helpers;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data
{
  public class ChargingSessionRepository : Repository<ChargingSession>, IChargingSessionRepository
  {
    private readonly IMapper _mapper;
    private readonly ILogger<ChargingSessionRepository> _logger;
    public ChargingSessionRepository(
        VoltaXApiDbContext context,
        ILogger<ChargingSessionRepository> logger,
        IMapper mapper) : base(context)
    {
      _logger = logger;
      _mapper = mapper;
    }

    public IQueryable<ChargePointChargingSessionListDto> GetChargePointChargingSessions(int chargePointID, GlobalParams globalParams)
    {
      var chargingSessions = _context.ChargingSessions.Include(u => u.User).Include(u => u.Connector)
          .Where(cs => cs.Connector.ChargePointID == chargePointID).AsQueryable().ProjectTo<ChargePointChargingSessionListDto>(_mapper.ConfigurationProvider);
      return chargingSessions;
    }

    public async Task<ChargingSession> GetLastChargingSession(int connectorID)
    {
      var lastCS = await this._context.ChargingSessions
              .Where(c => c.ConnectorID == connectorID && c.EndDate != null)
              .OrderByDescending(c => c.StartDate)
              .FirstOrDefaultAsync();
      return lastCS;
    }

    public async Task<ChargingSessionInformationsDto> GetChargingSessionInformations(int chargingSessionID)
    {
      _logger.LogInformation("Start VoltaxAPI.Data.GetChargingSessionInformations for chargingSessionID : " + chargingSessionID);
      var result = await _context.ChargingSessions
          .Where(cs => cs.ID == chargingSessionID)
          .Select(cs => new ChargingSessionInformationsDto
          {
            UserName = cs.User.FirstName + " " + cs.User.LastName,
            UserID = cs.User.ID,
            CardID = cs.CardID,
            Card = cs.Card,
            ChargePointID = cs.Connector.ChargePoint.ID,
            ChargePointName = cs.Connector.ChargePoint.ChargePointId,
            KwhCharged = cs.Transactions.Sum(t => (t.MeterStop ?? 0) - t.MeterStart),
            StartDate = cs.StartDate,
            IdleTimeRatio = (double)cs.Connector.PricePerIdleMinute,
            TimeRatio = (double)cs.Connector.PricePerMinute,
            IdleMinutes = cs.IdleMinutes,
            TotalPrice = 0,
            ChargingTimeInMinutes = cs.ChargedMinutes,
            EndDate = cs.EndDate,
            ConnectorID = cs.ConnectorID,
            ConnectorRatio = cs.Connector.PricePerKWh,
            Transactions = cs.Transactions.ToList(),
            ConnectorCostRatio = cs.Connector.CostPerKwh,
          })
          .FirstOrDefaultAsync();

      // Calculating Charging Time for charging session : 
      var transactionsTime = result.Transactions?.Any() == true
        ? result.Transactions.Sum(t =>
            ((t.StopTime ?? DateTime.UtcNow) - t.StartTime).TotalMinutes) : 0;

      result.IdleTimePrice = result.IdleMinutes * result.IdleTimeRatio;

      result.TotalPrice += result.IdleTimePrice +  (result.TimeRatio * result.ChargingTimeInMinutes);

      return result;
    }

    public async Task<List<int>> GetChargePointChargingSessionsIDs(int chargePointID) =>
        await _context.ChargingSessions.Where(u => u.Connector.ChargePointID == chargePointID).Select(u => u.ID).ToListAsync();

    public async Task<int> GetChargePointNbrChargingSessions(int chargePointID)
    {
      return await _context.ChargingSessions.CountAsync(cs => cs.Connector.ChargePointID == chargePointID);
    }

    public async Task<int> GetChargePointNbrChargingSessionsToday(int chargePointID)
    {
      return await _context.ChargingSessions.Where(cs => cs.StartDate >= DateTime.Now.AddDays(-1)).CountAsync(cs => cs.Connector.ChargePointID == chargePointID);
    }

    public IQueryable<ChargingSessionListDto> GetChargingSessions()
    {
      return _context.ChargingSessions.Select(cs => new ChargingSessionListDto
      {
        ID = cs.ID,
        Connector = cs.Connector.EvseID + " " + cs.Connector.ConnectorID,
        ConnectorID = cs.ConnectorID,
        ChargePointID = cs.Connector.ChargePointID,
        UserName = cs.User.FullName,
        CardNumber = cs.Card.CardNumber,
        StartDate = cs.StartDate,
        EndDate = cs.EndDate,
        StoppedReason = cs.StoppedReason,
        ChargingSessionStatus = cs.ChargingSessionStatus,
      }).AsQueryable();
    }


    public async Task<Dictionary<DateTime, double>> GetChargePointNbrChargingSessionsLast30Days(int chargePointID)
    {
      var chargingSessions = await _context.ChargingSessions
          .Where(t => t.StartDate >= DateTime.Today.AddDays(-30))
          .ToListAsync();

      var nbrChargingSessionsByDay = new Dictionary<DateTime, double>();
      foreach (var transaction in chargingSessions)
      {
        var date = transaction.StartDate.Date;

        if (nbrChargingSessionsByDay.ContainsKey(date))
        {
          nbrChargingSessionsByDay[date] += 1;
        }
        else
        {
          nbrChargingSessionsByDay[date] = 1;
        }
      }

      return nbrChargingSessionsByDay;
    }

    public IQueryable<ChargingSessionListDto> GetUserChargingSessions(int userID, GlobalParams globalParams)
    {
      return GetAllAsync(globalParams).Where(u => u.UserID == userID).Select(cs => new ChargingSessionListDto
      {
        ID = cs.ID,
        Connector = cs.Connector.EvseID + " " + cs.Connector.ConnectorID,
        ConnectorID = cs.ConnectorID,
        ChargePointID = cs.Connector.ChargePointID,
        UserName = cs.User.FullName,
        CardNumber = cs.Card.CardNumber,
        StartDate = cs.StartDate,
        EndDate = cs.EndDate,
        StoppedReason = cs.StoppedReason,
        ChargingSessionStatus = cs.ChargingSessionStatus,
      }).AsQueryable();
    }

    
    public async Task<ChargingSession> CreateChargingSessionForTransaction(Connector connector, Card card, DateTime startDate)
    {
      ChargingSession chargingSession = new()
      {
        ConnectorID = connector.ID,
        UserID = (int)(card.UserID == null ? 1 : card.UserID),
        CardID = card.ID,
        StartDate = startDate,
        StoppedReason = ReasonEnumType.Local,
        ChargingSessionStatus = ChargingSessionStatusEnum.Pending
      };

      await this.AddAsync(chargingSession);

      return chargingSession;
    }
  }


}