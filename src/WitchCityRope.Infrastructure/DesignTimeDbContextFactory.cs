using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure
{
    /// <summary>
    /// Design-time factory for Entity Framework Core migrations
    /// This is used by the EF Core tools to create migrations at design time
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WitchCityRopeDbContext>
    {
        public WitchCityRopeDbContext CreateDbContext(string[] args)
        {
            // Try to find API project configuration files
            var currentDir = Directory.GetCurrentDirectory();
            var apiProjectPath = Path.Combine(currentDir, "..", "WitchCityRope.Api");
            
            // If Infrastructure project is current directory, look for API project
            if (!Directory.Exists(apiProjectPath))
            {
                // Try relative path from Infrastructure project to Api project
                apiProjectPath = Path.Combine(currentDir, "..", "WitchCityRope.Api");
                if (!Directory.Exists(apiProjectPath))
                {
                    // Fallback to current directory
                    apiProjectPath = currentDir;
                }
            }

            // Build configuration from API project directory if it exists
            var configPath = Directory.Exists(apiProjectPath) ? apiProjectPath : currentDir;
            
            var configuration = new ConfigurationBuilder()
                .SetBasePath(configPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Create DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<WitchCityRopeDbContext>();
            
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Port=5433;Database=witchcityrope_dev;Username=postgres;Password=devpass123";
            
            optionsBuilder.UseNpgsql(connectionString);

            return new WitchCityRopeDbContext(optionsBuilder.Options);
        }
    }
}