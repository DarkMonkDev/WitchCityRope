using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Data
{
    /// <summary>
    /// Separate fixture for migration tests that doesn't use EnsureCreated
    /// </summary>
    public class MigrationTestFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _postgresContainer = null!;
        
        public string ConnectionString { get; private set; } = null!;
        
        public async Task InitializeAsync()
        {
            // Create a new PostgreSQL container for migration tests
            _postgresContainer = new PostgreSqlBuilder()
                .WithDatabase("witchcityrope_migration_test")
                .WithUsername("postgres")
                .WithPassword("test_password")
                .WithPortBinding(5434, 5432) // Use different port to avoid conflicts
                .Build();
            
            await _postgresContainer.StartAsync();
            
            ConnectionString = _postgresContainer.GetConnectionString();
        }
        
        public async Task DisposeAsync()
        {
            if (_postgresContainer != null)
            {
                await _postgresContainer.DisposeAsync();
            }
        }
        
        /// <summary>
        /// Creates a fresh database context for migration testing
        /// </summary>
        public WitchCityRopeIdentityDbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>();
            optionsBuilder.UseNpgsql(ConnectionString);
            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
            
            return new WitchCityRopeIdentityDbContext(optionsBuilder.Options);
        }
        
        /// <summary>
        /// Ensures the database is completely clean (no tables)
        /// </summary>
        public async Task EnsureCleanDatabaseAsync()
        {
            using var context = CreateContext();
            
            // Get all tables in the public and auth schemas
            var tables = await context.Database.ExecuteSqlRawAsync(@"
                DO $$ 
                DECLARE 
                    r RECORD;
                BEGIN
                    -- Drop all tables in public schema
                    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') 
                    LOOP
                        EXECUTE 'DROP TABLE IF EXISTS public.' || quote_ident(r.tablename) || ' CASCADE';
                    END LOOP;
                    
                    -- Drop all tables in auth schema if it exists
                    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'auth') THEN
                        FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'auth') 
                        LOOP
                            EXECUTE 'DROP TABLE IF EXISTS auth.' || quote_ident(r.tablename) || ' CASCADE';
                        END LOOP;
                    END IF;
                    
                    -- Drop auth schema if it exists
                    DROP SCHEMA IF EXISTS auth CASCADE;
                END$$;");
        }
    }
    
    [CollectionDefinition("Migration Tests")]
    public class MigrationTestCollection : ICollectionFixture<MigrationTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}