using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using WitchCityRope.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace WitchCityRope.IntegrationTests
{
    public class DirectTestContainerTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly PostgreSqlContainer _container;
        private bool _disposed = false;

        public DirectTestContainerTest(ITestOutputHelper output)
        {
            _output = output;
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
        }

        [Fact]
        public async Task Test_DirectContainerWithFactory_DatabaseInitialization()
        {
            _output.WriteLine("🚀 Starting direct container test...");

            await _container.StartAsync();
            _output.WriteLine("✅ Container started successfully");

            try
            {
                // Create the factory with our container
                var factory = new TestWebApplicationFactory(_container);
                _output.WriteLine("✅ Factory created successfully");

                // Try to create a client (this should trigger database initialization)
                _output.WriteLine("🔄 Creating client to trigger database initialization...");
                using var client = factory.CreateClient();
                _output.WriteLine("✅ Client created successfully");

                // Now try to access the database directly through the factory
                _output.WriteLine("🔍 Accessing database through factory...");
                using var scope = factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
                
                // Check if database can be accessed
                var canConnect = await context.Database.CanConnectAsync();
                _output.WriteLine($"🔌 Database connection: {canConnect}");
                
                // Check if any migrations are pending
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                _output.WriteLine($"📋 Pending migrations: {pendingMigrations.Count()}");
                
                // Check if applied migrations exist
                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                _output.WriteLine($"✅ Applied migrations: {appliedMigrations.Count()}");
                
                // Try to access some tables
                try
                {
                    var userCount = await context.Users.CountAsync();
                    _output.WriteLine($"👥 Users count: {userCount}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"❌ Error accessing Users table: {ex.Message}");
                }
                
                try
                {
                    var roleCount = await context.Roles.CountAsync();
                    _output.WriteLine($"🎭 Roles count: {roleCount}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"❌ Error accessing Roles table: {ex.Message}");
                }

                await factory.DisposeAsync();
                _output.WriteLine("✅ Factory disposed successfully");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Test failed: {ex.Message}");
                _output.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
            
            _output.WriteLine("🎉 Test completed successfully");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _container?.DisposeAsync().AsTask().Wait();
                _disposed = true;
            }
        }
    }
}