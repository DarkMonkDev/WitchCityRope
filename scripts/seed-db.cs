#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.EntityFrameworkCore, 9.0.0"
#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 9.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 9.0.0"
#r "nuget: Microsoft.AspNetCore.Identity.EntityFrameworkCore, 9.0.0"
#r "nuget: BCrypt.Net-Next, 4.0.3"
#load "../src/WitchCityRope.Core/Entities/*.cs"
#load "../src/WitchCityRope.Core/Enums/*.cs"
#load "../src/WitchCityRope.Core/ValueObjects/*.cs"
#load "../src/WitchCityRope.Infrastructure/Identity/*.cs"
#load "../src/WitchCityRope.Infrastructure/Data/WitchCityRopeIdentityDbContext.cs"
#load "../src/WitchCityRope.Infrastructure/Data/DbInitializer.cs"

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;

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
var optionsBuilder = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>();
optionsBuilder.UseNpgsql(connectionString);

// Create context and seed database
try
{
    using var context = new WitchCityRopeIdentityDbContext(optionsBuilder.Options);
    
    logger.LogInformation("Connecting to database...");
    await context.Database.EnsureCreatedAsync();
    
    logger.LogInformation("Database seeding with WitchCityRopeIdentityDbContext...");
    // Note: This would need UserManager and RoleManager for proper seeding
    // For now, just ensure database is created
    
    logger.LogInformation("Database setup completed successfully!");
    logger.LogInformation("Note: Use the DatabaseSeeder tool for full seeding with Identity support.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error setting up database");
    throw;
}