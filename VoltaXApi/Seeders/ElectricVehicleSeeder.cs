using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VoltaXApi.Data;
using VoltaXApi.Models;

public static class ElectricVehicleModelSeeder
{

    public static async Task SeedAsync(VoltaXApiDbContext context,string csvFilePath)
    {
        if (context.ElectricVehicleModels.Any())
            return; // Prevent duplicate seeding

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            TrimOptions = TrimOptions.Trim
        };


        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<CarCsv>().ToList();

        var cars = records.Select(r => new ElectricVehicleModel
        {
            Model = r.Model,
            Make = r.Make,
            Type = r.Type,
            Range = r.Range,
            BatteryCapacity = r.BatteryCapacity,
            ImageSrc = r.ImageSrc,
            LogoSrc = r.LogoSrc
        }).ToList();

        context.ElectricVehicleModels.AddRange(cars);
        await context.SaveChangesAsync();
    }
}

internal class CsvConfiguration
{
    private CultureInfo invariantCulture;

    public CsvConfiguration(CultureInfo invariantCulture)
    {
        this.invariantCulture = invariantCulture;
    }

    public bool HasHeaderRecord { get; set; }
    public string Delimiter { get; set; }
    public object TrimOptions { get; set; }
}

public class CarCsv
{
    public string Model { get; set; }
    public string Make { get; set; }
    public string Type { get; set; }
    public string Range { get; set; }
    public string BatteryCapacity { get; set; }
    public string ImageSrc { get; set; }
    public string LogoSrc { get; set; }
}
