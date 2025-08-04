using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.IntegrationTests
{
    [Collection("PostgreSQL Integration Tests")]
    public class DebugDatabaseSetup : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public DebugDatabaseSetup(PostgreSqlFixture postgresFixture, ITestOutputHelper output) 
            : base(postgresFixture)
        {
            _output = output;
        }

        [Fact]
        public async Task Debug_TestWebApplicationFactory_Initialization()
        {
            _output.WriteLine("🔍 Starting TestWebApplicationFactory debug test...");
            
            try
            {
                // Try to get the database context
                using var scope = Factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
                
                _output.WriteLine("✅ Successfully retrieved DbContext");
                
                // Check if database can be accessed
                var canConnect = await context.Database.CanConnectAsync();
                _output.WriteLine($"🔌 Database connection: {canConnect}");
                
                // Check if any tables exist
                var tables = await context.Database.GetPendingMigrationsAsync();
                _output.WriteLine($"📋 Pending migrations: {tables.Count()}");
                
                // Check if users table exists
                try
                {
                    var userCount = await context.Users.CountAsync();
                    _output.WriteLine($"👥 Users count: {userCount}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"❌ Error accessing Users table: {ex.Message}");
                }
                
                // Check if roles table exists
                try
                {
                    var roleCount = await context.Roles.CountAsync();
                    _output.WriteLine($"🎭 Roles count: {roleCount}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"❌ Error accessing Roles table: {ex.Message}");
                }
                
                // Check if events table exists
                try
                {
                    var eventCount = await context.Events.CountAsync();
                    _output.WriteLine($"🎉 Events count: {eventCount}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"❌ Error accessing Events table: {ex.Message}");
                }
                
                _output.WriteLine("✅ Debug test completed successfully");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Debug test failed: {ex.Message}");
                _output.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}