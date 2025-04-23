using System;
using System.Collections.Generic;
using AutoMapper;
using Bogus;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class DatabaseInit
    {
        public static async Task Seed(VoltaXApiDbContext context, IMapper mapper)
        {
            await SqlScriptExecuter.ExecuteSqlScript(context);
            await ChargePointBrandsSeeder.Seed(context,mapper);
            await BrandsAutomobilesSeeder.Populate(context);
            var users = await UserSeeder.Seed(20,context);
        }
    }
}
