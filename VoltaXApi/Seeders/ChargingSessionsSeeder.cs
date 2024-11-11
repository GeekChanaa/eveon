using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AutoMapper;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargingSessionsSeeder
    {
      public async static Task Seed(int count, VoltaXApiDbContext dbContext)
      {
          var faker = new Faker<ChargingSession>()
                .RuleFor(cs => cs.ChargePointID, f => f.Random.Number(1, 100)) 
                .RuleFor(cs => cs.ConnectorID, f => f.Random.Number(1, 10)) 
                .RuleFor(cs => cs.UserID, f => f.Random.Number(1, 50)) 
                .RuleFor(cs => cs.CardID, f => f.Random.Number(1, 100)) 
                .RuleFor(cs => cs.StartDate, f => f.Date.Past(1)) 
                .RuleFor(cs => cs.EndDate, (f, cs) => f.Date.Between(cs.StartDate, DateTime.Now)) 
                .RuleFor(cs => cs.StoppedReason, f => f.PickRandom<ReasonEnumType>()) 
                .RuleFor(cs => cs.ChargingSessionStatus, f => f.PickRandom<ChargingSessionStatusEnum>()) 
                .RuleFor(cs => cs.IsDeleted, f => f.Random.Bool()) 
                .RuleFor(cs => cs.CreatedAt, f => f.Date.Recent()) 
                .RuleFor(cs => cs.UpdatedAt, (f, cs) => f.Date.Between(cs.CreatedAt, DateTime.Now)); 

            
            var chargingSessions = faker.Generate(count);

            await dbContext.ChargingSessions.AddRangeAsync(chargingSessions);
            await dbContext.SaveChangesAsync();

      }
    }
}
