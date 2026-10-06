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
    private readonly VoltaXApi.Services.IBusinessClock _clock;
    public ChargingSessionRepository(
        VoltaXApiDbContext context,
        ILogger<ChargingSessionRepository> logger,
        GlobalConfigurations globalConfigurations,
        IMapper mapper,
        VoltaXApi.Services.IBusinessClock clock) : base(context)
    {
      _clock = clock;
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
            KwhCharged = cs.ChargedKwhs,
            CostPerKwh = cs.CostPerKwh,
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
      var from = _clock.StartOfDayUtc(_clock.Today);
      return await _context.ChargingSessions.CountAsync(cs => cs.StartDate >= from && cs.Connector!.ChargePointID == chargePointID);
    }

    public IQueryable<ChargingSessionListDto> GetChargingSessions()
    {
      return _context.ChargingSessions.Select(cs => new ChargingSessionListDto
      {
        ID = cs.ID,
        Connector = cs.Connector.EvseID + " " + cs.Connector.ConnectorID,
        ConnectorID = cs.ConnectorID,
        ChargePointID = cs.Connector.ChargePointID,
        ChargePointName =  cs.Connector.ChargePoint.ChargePointId,
        UserName = cs.User.FullName,
        CardNumber = cs.Card.CardNumber,
        StartDate = cs.StartDate,
        EndDate = cs.EndDate,
        StoppedReason = cs.StoppedReason,
        ChargedMinutes = cs.ChargedMinutes,
        IdleMinutes = cs.IdleMinutes,
        ChargedKwhs = cs.ChargedKwhs,
        PricePerMinute = cs.PricePerMinute,
        CostPerKwh = cs.CostPerKwh,
        PricePerIdleMinute = cs.PricePerIdleMinute,
        TotalPriceWithVAT = cs.TotalPriceWithVAT(_globalConfigurations.Vat, (int)_globalConfigurations.GracePeriod),
        ChargingSessionStatus = cs.ChargingSessionStatus,
      }).AsQueryable();
    }

    public IQueryable<PartnerChargingSessionListDto> GetPartnerChargingSessions(int partnerID)
    {
      return _context.ChargingSessions.Where(u => u.Connector.ChargePoint.ChargingStation.PartnerID == partnerID).Select(cs => new PartnerChargingSessionListDto
      {
        ID = cs.ID,
        Connector = cs.Connector.EvseID + " " + cs.Connector.ConnectorID,
        ConnectorID = cs.ConnectorID,
        ChargePointName =  cs.Connector.ChargePoint.ChargePointId,
        ChargePointID = cs.Connector.ChargePointID,
        StartDate = cs.StartDate,
        EndDate = cs.EndDate,
        StoppedReason = cs.StoppedReason,
        IdleMinutes = cs.IdleMinutes,
        ChargedMinutes = cs.ChargedMinutes,
        ChargedKwhs = cs.ChargedKwhs,
        TotalPriceWithVAT = cs.TotalPriceWithVAT(_globalConfigurations.Vat, (int)_globalConfigurations.GracePeriod),
        ChargingSessionStatus = cs.ChargingSessionStatus,
      }).AsQueryable();
    }
    


    public async Task<Dictionary<DateTime, double>> GetChargePointNbrChargingSessionsLast30Days(int chargePointID)
    {
      var from = _clock.StartOfDayUtc(_clock.Today.AddDays(-30));
      var startDates = await _context.ChargingSessions.AsNoTracking()
          .Where(cs => cs.StartDate >= from && cs.Connector!.ChargePointID == chargePointID)
          .Select(cs => cs.StartDate)
          .ToListAsync();

      return startDates
          .GroupBy(date => _clock.ToLocal(date).Date)
          .ToDictionary(g => g.Key, g => (double)g.Count());
    }

    public IQueryable<ChargingSessionListDto> GetUserChargingSessions(int userID, GlobalParams globalParams)
    {
      // The soft-delete filters on Connector / ChargePoint / Card turn their joins into INNER JOINs
      // that run *after* SQL Server has paged the sessions, so a page whose sessions sit on a
      // deleted connector came back empty while the count still included them. A user's history
      // keeps those sessions: the filters are lifted and only the session's own flag is checked.
      var sessions = GetAllAsync(globalParams)
        .IgnoreQueryFilters()
        .Where(cs => !cs.IsDeleted && cs.UserID == userID);

      // Without an explicit order, OFFSET/FETCH pages over an arbitrary order.
      if (string.IsNullOrEmpty(globalParams.OrderBy))
        sessions = sessions.OrderByDescending(cs => cs.StartDate).ThenByDescending(cs => cs.ID);

      return sessions.Select(cs => new ChargingSessionListDto
      {
        ID = cs.ID,
        Connector = cs.Connector.EvseID + " " + cs.Connector.ConnectorID,
        ChargePointName = cs.Connector.ChargePoint.ChargePointId,
        ConnectorID = cs.ConnectorID,
        ChargePointID = cs.Connector.ChargePointID,
        // FullName is a C# property: using it made EF load the whole user row, password hash included.
        UserName = cs.User.FirstName + " " + cs.User.LastName,
        CardNumber = cs.Card.CardNumber,
        StartDate = cs.StartDate,
        EndDate = cs.EndDate,
        ChargedMinutes = cs.ChargedMinutes,
        IdleMinutes = cs.IdleMinutes,
        ChargedKwhs = cs.ChargedKwhs,
        PricePerMinute = cs.PricePerMinute,
        CostPerKwh = cs.CostPerKwh,
        PricePerIdleMinute = cs.PricePerIdleMinute,
        TotalPriceWithVAT = cs.TotalPriceWithVAT(_globalConfigurations.Vat, (int)_globalConfigurations.GracePeriod),
        StoppedReason = cs.StoppedReason,
        
        ChargingSessionStatus = cs.ChargingSessionStatus,
      }).AsQueryable();
    }

    public IQueryable<MyChargingSessionDto> GetMyChargingSessions(int userID)
    {
      return _context.ChargingSessions
          .AsNoTracking()
          .Where(session => session.UserID == userID)
          .OrderByDescending(session => session.StartDate)
          .ThenByDescending(session => session.ID)
          .Select(session => new MyChargingSessionDto
          {
            ID = session.ID,
            StationName = session.Connector != null &&
                session.Connector.ChargePoint != null &&
                session.Connector.ChargePoint.ChargingStation != null
                    ? session.Connector.ChargePoint.ChargingStation.Name
                    : null,
            StationAddress = session.Connector != null &&
                session.Connector.ChargePoint != null &&
                session.Connector.ChargePoint.ChargingStation != null
                    ? session.Connector.ChargePoint.ChargingStation.Address
                    : null,
            ChargePointName = session.Connector != null && session.Connector.ChargePoint != null
                ? session.Connector.ChargePoint.ChargePointId
                : null,
            Connector = session.Connector != null
                ? "EVSE " + session.Connector.EvseID + " / Connector " + session.Connector.ConnectorID
                : null,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            ChargedMinutes = session.ChargedMinutes ?? 0,
            IdleMinutes = session.IdleMinutes ?? 0,
            ChargedKwhs = session.ChargedKwhs ?? 0,
            TotalPriceWithVAT = session.TotalPriceWithVAT(
                _globalConfigurations.Vat,
                (int)_globalConfigurations.GracePeriod),
            StoppedReason = session.StoppedReason,
            Status = session.ChargingSessionStatus,
            InvoiceAvailable = session.EndDate.HasValue
          });
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
        CostPerKwh = connector.CostPerKwh,
      };

      await this.AddAsync(chargingSession);

      return chargingSession;
    } 
    
    public async Task<ChargingSessionInformationsDto?> GetUserCurrentChargingSession(int userID, GlobalParams globalParams)
    {
      var chargingSession = await _context.ChargingSessions
          .Where(u => u.UserID == userID && u.EndDate == null)
          .Select(cs => new ChargingSessionInformationsDto
          {
              ID = cs.ID,
              ChargePointID = cs.Connector.ChargePointID,
              ChargePointName = cs.Connector.ChargePoint.ChargePointId,
              UserName = cs.User.FullName,
              UserID = cs.UserID,
              CardID = cs.CardID,
              ConnectorID = cs.ConnectorID,
              ChargedMinutes = cs.ChargedMinutes,
              IdleMinutes = cs.IdleMinutes,
              PricePerMinute = cs.PricePerMinute,
              PricePerIdleMinute = cs.PricePerIdleMinute,
              ChargingPriceWithoutVAT = cs.ChargingPriceWithoutVAT(_globalConfigurations.Vat),
              ChargingPriceWithVAT = cs.ChargingPriceWithVAT,
              IdlePriceWithoutVAT = cs.IdleChargingPriceWithoutVAT(_globalConfigurations.Vat, (int)_globalConfigurations.GracePeriod),
              IdldePriceWithVAT = cs.IdleChargingPriceWithVAT((int)_globalConfigurations.GracePeriod),
              TotalPriceWithoutVAT = cs.TotalPriceWithoutVAT(_globalConfigurations.Vat, (int)_globalConfigurations.GracePeriod),
              TotalPriceWithVAT = cs.TotalPriceWithVAT(_globalConfigurations.Vat, (int)_globalConfigurations.GracePeriod),
              KwhCharged = cs.ChargedKwhs,
              CostPerKwh = cs.CostPerKwh,
              StartDate = cs.StartDate,
              StoppedReason = cs.StoppedReason,
              ChargingSessionStatus = cs.ChargingSessionStatus,
          })
          .FirstOrDefaultAsync();

      return chargingSession;
    }

    
  }


}
