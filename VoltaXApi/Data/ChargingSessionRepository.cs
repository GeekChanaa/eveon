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
      return await this._context.ChargingSessions
              .Where(c => c.ConnectorID == connectorID && c.EndDate != null)
              .OrderByDescending(c => c.StartDate)
              .FirstOrDefaultAsync();
    }

    public async Task<ChargingSessionInformationsDto> GetChargingSessionInformations(int chargingSessionID)
    {
      _logger.LogInformation("Start VoltaxAPI.Data.GetChargingSessionInformations for chargingSessionID : "+chargingSessionID);
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
            TotalPrice = cs.Transactions.Sum(t => t.Amount),
            KwhCharged = cs.Transactions.Sum(t => (t.MeterStop ?? 0) - t.MeterStart),
            StartDate = cs.StartDate,
            IdleTimeRatio = cs.Connector.PricePerIdleMinute,
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
            ((t.StopTime ?? DateTime.UtcNow) - t.StartTime).TotalMinutes): 0;

      result.ChargingTimeInMinutes = transactionsTime;
      // Get All connectorUptimes for these transactions
      var connectorUptimes = _context.ConnectorUptimes
          .Where(u => u.ConnectorUptimeStatus == ConnectorUptimeStatusEnum.Charging)
          .Where(u => result.Transactions.Select(u => u.ID).ToList().Contains(u.TransactionID ?? 0))
          .ToList();

      var totalConnectedTime = connectorUptimes.Sum(cu => ((cu.EndDate ?? DateTime.UtcNow) - cu.StartDate).TotalMinutes);

      var idleTime = (int)(totalConnectedTime - transactionsTime);
      if (idleTime < 5)
        result.IdleMinutes = 0;
      else
        result.IdleMinutes = (decimal)idleTime;

      result.IdleTimePrice = result.IdleMinutes * result.IdleTimeRatio;

      result.TotalPrice += (double)result.IdleTimePrice + ((double)result.ConnectorRatio * result.KwhCharged);

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
      return _context.ChargingSessions.Select(cs => new ChargingSessionListDto{
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
          .Where(t =>t.StartDate >= DateTime.Today.AddDays(-30))
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
      return GetAllAsync(globalParams).Where(u => u.UserID == userID).Select(cs => new ChargingSessionListDto{
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

    public async Task<ChargingSession> StartChargingSession(Connector connector, Card card)
    {
      ChargingSession chargingSession = new()
      {
        ConnectorID = connector.ID,
        UserID = (int)(card.UserID == null ? 1 : card.UserID),
        CardID = card.ID,
        StartDate = DateTime.Now,
        StoppedReason = ReasonEnumType.Local,
        ChargingSessionStatus = ChargingSessionStatusEnum.Pending
      };
        
      await this.AddAsync(chargingSession);

      return chargingSession;

    }


  }

  
}