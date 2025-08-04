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
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Create DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<WitchCityRopeDbContext>();
            
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Database=witchcityrope_db;Username=postgres;Password=your_password_here";
            
            optionsBuilder.UseNpgsql(connectionString);

            return new WitchCityRopeDbContext(optionsBuilder.Options);
        }
    }
}