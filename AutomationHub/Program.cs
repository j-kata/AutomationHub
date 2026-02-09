using AutomationHub.Core.Interfaces;
using AutomationHub.Core.Services;
using AutomationHub.Infrastructure.Extensions;
using AutomationHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IEventProcessor, EventProcessor>();
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddActionHandlers();
builder.Services.AddApplicationContext(builder.Configuration);

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
