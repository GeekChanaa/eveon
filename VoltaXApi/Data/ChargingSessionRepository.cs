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
    private readonly GlobalConfigurations _globalConfigurations;
    public ChargingSessionRepository(
        VoltaXApiDbContext context,
        ILogger<ChargingSessionRepository> logger,
        GlobalConfigurations globalConfigurations,
        IMapper mapper) : base(context)
    {
      _logger = logger;
      _mapper = mapper;
      _globalConfigurations = globalConfigurations;
    }

    public IQueryable<ChargePointChargingSessionListDto> GetChargePointChargingSessions(int chargePointID, GlobalParams globalParams)
    {
      var chargingSessions = _context.ChargingSessions.Include(u => u.User).Include(u => u.Connector)
          .Where(cs => cs.Connector.ChargePointID == chargePointID).AsQueryable().ProjectTo<ChargePointChargingSessionListDto>(_mapper.ConfigurationProvider);
      return chargingSessions;
    }

    public async Task<ChargingSession> GetLastChargingSession(int connectorID)
    {
      var lastCS = await _context.ChargingSessions
          .Where(c => c.ConnectorID == connectorID && c.EndDate != null)
          .OrderByDescending(c => c.StartDate)
          .ThenByDescending(c => c.ID) 
          .FirstOrDefaultAsync();
      return lastCS;
    }

    public async Task<ChargingSessionInformationsDto> GetChargingSessionInformations(int chargingSessionID)
    {
      _logger.LogInformation("Start VoltaxAPI.Data.GetChargingSessionInformations for chargingSessionID : " + chargingSessionID);
      var vatRate = _globalConfigurations.Vat;
      var gracePeriodSeconds = _globalConfigurations.GracePeriod;
      var chargingSession = await _context.ChargingSessions
          .Where(cs => cs.ID == chargingSessionID)
          .Select(cs => new ChargingSessionInformationsDto
          {
            ID = cs.ID,
            ChargePointID = cs.Connector != null ? cs.Connector.ChargePointID : null,
            ChargePointName = cs.Connector != null && cs.Connector.ChargePoint != null
                ? cs.Connector.ChargePoint.ChargePointId
                : null,
            UserName = cs.User != null ? cs.User.FullName : null,
            UserID = cs.UserID,
            CardID = cs.CardID,
            CardNumber = cs.Card.CardNumber,
            CardBalance = cs.Card.Balance,
            ConnectorID = cs.ConnectorID,
            ChargedMinutes = cs.ChargedMinutes,
            IdleMinutes = cs.IdleMinutes,
            PricePerMinute = cs.PricePerMinute,
            PricePerIdleMinute = cs.PricePerIdleMinute,
            ChargingPriceWithoutVAT = cs.ChargingPriceWithoutVAT(vatRate),
            ChargingPriceWithVAT = cs.ChargingPriceWithVAT,
            IdlePriceWithoutVAT = cs.IdleChargingPriceWithoutVAT(vatRate,(int)gracePeriodSeconds),
            IdldePriceWithVAT = cs.IdleChargingPriceWithVAT((int) gracePeriodSeconds),
            TotalPriceWithoutVAT = cs.TotalPriceWithoutVAT(vatRate,(int) gracePeriodSeconds),
            TotalPriceWithVAT = cs.TotalPriceWithVAT(vatRate,(int) gracePeriodSeconds),
            KwhCharged = 0,
            StartDate = cs.StartDate,
            EndDate = cs.EndDate,
            StoppedReason = cs.StoppedReason,
            ChargingSessionStatus = cs.ChargingSessionStatus,
          })
          .FirstOrDefaultAsync();

      return chargingSession;
    }

    public async Task<ChargingSessionForMailDto> GetChargingSessionForMail(int chargingSessionID)
    {
      _logger.LogInformation("Start VoltaxAPI.Data.GetChargingSessionInformations for chargingSessionID : " + chargingSessionID);
      var vatRate = _globalConfigurations.Vat;
      var gracePeriodSeconds = _globalConfigurations.GracePeriod;
      var chargingSession = await _context.ChargingSessions
          .Where(cs => cs.ID == chargingSessionID)
          .Select(cs => new ChargingSessionForMailDto
          {
            ID = cs.ID,
            ChargePointName = cs.Connector.ChargePoint.ChargePointId,
            UserName = cs.User.FullName,
            UserID = cs.UserID,
            ConnectorID = cs.ConnectorID,
            ConnectorType = cs.Connector.ConnectorType.ToString(),
            ConnectorPower = cs.Connector.Power,
            ChargedMinutes =  cs.ChargedMinutes,
            IdleMinutes = cs.IdleMinutes, 
            PricePerMinute = cs.PricePerMinute,
            PricePerIdleMinute = cs.PricePerIdleMinute,
            ChargingPriceWithoutVAT = cs.ChargingPriceWithoutVAT(vatRate),
            ChargingPriceWithVAT = cs.ChargingPriceWithVAT,
            IdlePriceWithoutVAT = cs.IdleChargingPriceWithoutVAT(vatRate,(int)gracePeriodSeconds),
            IdlePriceWithVAT = cs.IdleChargingPriceWithVAT((int)gracePeriodSeconds),
            TotalPriceWithoutVAT = cs.TotalPriceWithoutVAT(vatRate,(int)gracePeriodSeconds),
            TotalPriceWithVAT = cs.TotalPriceWithVAT(vatRate,(int)gracePeriodSeconds),
            KwhCharged = 0,
            StartDate = cs.StartDate,
            EndDate = cs.EndDate,
            StoppedReason = cs.StoppedReason,
            ChargingSessionStatus = cs.ChargingSessionStatus,
          })
          .FirstOrDefaultAsync();

      return chargingSession;
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
        ChargingSessionStatus = ChargingSessionStatusEnum.Pending,
        PricePerIdleMinute = connector.PricePerIdleMinute,
        PricePerMinute = connector.PricePerMinute,
      };

      await this.AddAsync(chargingSession);

      return chargingSession;
    }
  }


}