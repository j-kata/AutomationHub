using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Services;
using AutomationHub.Infrastructure.Extensions;
using AutomationHub.Infrastructure.Data.Repositories;
using AutomationHub.Infrastructure.Adapters.Inbound;
using AutomationHub.Infrastructure.Options;
using AutomationHub.Infrastructure.Adapters.Inbound.MqttParsers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IEventProcessor, EventProcessor>();
builder.Services.AddScoped<IRuleRepository, RuleDbRepository>();
builder.Services.AddActionHandlers();
builder.Services.AddApplicationContext(builder.Configuration);
builder.Services.AddOptions<MqttOptions>()
    .Bind(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddSingleton<IMqttParser, TemperatureSensorParser>();
builder.Services.AddSingleton<IMqttParser, HumiditySensorParser>();
builder.Services.AddSingleton<IMqttParser, MotionSensorParser>();
builder.Services.AddHostedService<MqttAdapter>();

var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDatabaseAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
