#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.EntityFrameworkCore, 9.0.0"
#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 9.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 9.0.0"
#r "nuget: BCrypt.Net-Next, 4.0.3"
#load "../src/WitchCityRope.Core/Entities/*.cs"
#load "../src/WitchCityRope.Core/Enums/*.cs"
#load "../src/WitchCityRope.Core/ValueObjects/*.cs"
#load "../src/WitchCityRope.Infrastructure/Data/WitchCityRopeDbContext.cs"
#load "../src/WitchCityRope.Infrastructure/Data/DbInitializer.cs"

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Infrastructure.Data;

// Configure logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("DatabaseSeeder");

// Connection string - using the postgres container
var connectionString = "Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!";

// Configure DbContext
var optionsBuilder = new DbContextOptionsBuilder<WitchCityRopeDbContext>();
optionsBuilder.UseNpgsql(connectionString);

// Create context and seed database
try
{
    using var context = new WitchCityRopeDbContext(optionsBuilder.Options);
    
    logger.LogInformation("Connecting to database...");
    await context.Database.EnsureCreatedAsync();
    
    logger.LogInformation("Running database initializer...");
    await DbInitializer.InitializeAsync(context, logger);
    
    logger.LogInformation("Database seeding completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error seeding database");
    throw;
}