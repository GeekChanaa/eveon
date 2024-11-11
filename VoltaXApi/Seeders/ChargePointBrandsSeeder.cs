using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AutoMapper;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Data.Seeders
{
    public static class ChargePointBrandsSeeder
    {
        public async static Task Seed(VoltaXApiDbContext dbContext, IMapper mapper)
        {
            var jsonPath = "sql-scripts/charger-brands.json";
            var jsonData = File.ReadAllText(jsonPath);
            var brands = JsonConvert.DeserializeObject<List<ChargePointBrandSeederDto>>(jsonData);
            var brandsConverted = mapper.Map<List<ChargePointBrandSeederDto>, List<ChargePointBrand>>(brands);
            await dbContext.ChargePointBrands.AddRangeAsync(brandsConverted);
            await dbContext.SaveChangesAsync();

            // Fixing charge point models' image URLs after seeding
            await FixChargePointModelImageUrls(dbContext);
        }

        private static async Task FixChargePointModelImageUrls(VoltaXApiDbContext dbContext)
        {
            // Get all ChargePointModels from the database
            var chargePointModels = await dbContext.ChargePointModels
                .Include(c => c.ChargePointBrand) 
                .ToListAsync();

            foreach (var chargePointModel in chargePointModels)
            {
                
                var sanitizedModelName = SanitizeFilename(chargePointModel.Name.Replace(" ", "_"));
                var brandName = chargePointModel.ChargePointBrand?.Name ?? "UnknownBrand";
                chargePointModel.ImageUrl = $"/assets/images/models/{brandName}/{sanitizedModelName}.jpg";
                dbContext.ChargePointModels.Update(chargePointModel);
            }

            // Save the updated models to the database
            await dbContext.SaveChangesAsync();
        }

        // Sanitize filename by replacing invalid characters
        private static string SanitizeFilename(string filename)
        {
            // Replace invalid characters with '_'
            return Regex.Replace(filename, @"[\\/*?:""<>\|]", "_");
        }
    }
}
