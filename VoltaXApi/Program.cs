using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Data.Seeders;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Server;
using VoltaXApi.Helpers;
using System.Text.Json.Serialization;
using VoltaXApi.Mappers;
using Microsoft.Extensions.FileProviders;
using VoltaXApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<IStateRepository, StateRepository>();
builder.Services.AddScoped<IChargePointRepository, ChargePointRepository>();
builder.Services.AddScoped<IChargingStationRepository, ChargingStationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IConnectorStatusRepository, ConnectorStatusRepository>();
builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IFileManagementService, FileManagementService>();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddAutoMapper(typeof(ChargingStationProfile));
builder.Services.AddAutoMapper(typeof(UserProfile));
builder.Services.AddDbContext<VoltaXApiDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    );
            });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}





// app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthorization();

app.MapControllers();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
                    Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "/StaticFiles",
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
    // use context
    //  GlobalSeeder.Seed(dbContext).Wait();
    dbContext.Database.SetCommandTimeout(6000);

    //  await SqlScriptExecuter.ExecuteSqlScript();
}

// Set WebSocketsOptions
var webSocketOptions = new WebSocketOptions()
{
    ReceiveBufferSize = 8 * 1024
};

// Accept WebSocket
app.UseWebSockets(webSocketOptions);

// Integrate custom OCPP middleware for message processing
app.UseOCPPMiddleware();
app.Run();
