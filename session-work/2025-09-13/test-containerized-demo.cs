// Demo: Testing Containerized Infrastructure
// This demonstrates how TestContainers works in WitchCityRope

using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.ContainerizedTestDemo
{
    public class TestContainersDemo : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly PostgreSqlContainer _container;
        
        public TestContainersDemo(ITestOutputHelper output)
        {
            _output = output;
            
            // Configure PostgreSQL container
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")  // Same as production
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .WithPortBinding(0, 5432)  // Dynamic port allocation
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilCommandIsCompleted("pg_isready"))
                .Build();
        }
        
        public async Task InitializeAsync()
        {
            _output.WriteLine("🚀 Starting PostgreSQL container...");
            await _container.StartAsync();
            
            _output.WriteLine($"✅ Container started!");
            _output.WriteLine($"   Container ID: {_container.Id}");
            _output.WriteLine($"   Connection: {_container.GetConnectionString()}");
        }
        
        [Fact]
        public async Task Should_Connect_To_Containerized_PostgreSQL()
        {
            // Arrange
            var connectionString = _container.GetConnectionString();
            
            // Act & Assert
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT version()";
            var version = await command.ExecuteScalarAsync();
            
            _output.WriteLine($"📦 PostgreSQL Version: {version}");
            Assert.Contains("PostgreSQL 16", version.ToString());
        }
        
        [Fact]
        public async Task Should_Create_And_Query_Table()
        {
            // Arrange
            var connectionString = _container.GetConnectionString();
            
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Act - Create table
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = @"
                CREATE TABLE events (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100),
                    date TIMESTAMP
                )";
            await createCmd.ExecuteNonQueryAsync();
            
            // Act - Insert data
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO events (name, date) 
                VALUES ('Rope Workshop', '2025-09-15')";
            await insertCmd.ExecuteNonQueryAsync();
            
            // Act - Query data
            using var queryCmd = connection.CreateCommand();
            queryCmd.CommandText = "SELECT COUNT(*) FROM events";
            var count = await queryCmd.ExecuteScalarAsync();
            
            // Assert
            Assert.Equal(1L, count);
            _output.WriteLine("✅ Database operations successful!");
        }
        
        public async Task DisposeAsync()
        {
            _output.WriteLine("🧹 Cleaning up container...");
            await _container.DisposeAsync();
            _output.WriteLine("✅ Container cleaned up!");
        }
    }
    
    // This demonstrates the collection pattern for container reuse
    [CollectionDefinition("PostgreSQL Integration Tests")]
    public class PostgreSQLTestCollection : ICollectionFixture<DatabaseTestFixture>
    {
        // This class has no code, and is never created.
        // Its purpose is to be the place to apply [CollectionDefinition]
        // and all the ICollectionFixture<> interfaces.
    }
    
    public class DatabaseTestFixture : IAsyncLifetime
    {
        public PostgreSqlContainer Container { get; private set; }
        
        public async Task InitializeAsync()
        {
            Container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();
                
            await Container.StartAsync();
        }
        
        public async Task DisposeAsync()
        {
            await Container.DisposeAsync();
        }
    }
}