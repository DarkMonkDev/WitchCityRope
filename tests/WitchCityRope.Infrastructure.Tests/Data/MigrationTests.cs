using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WitchCityRope.Infrastructure.Data;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Data
{
    [Collection("Migration Tests")]
    public class MigrationTests
    {
        private readonly MigrationTestFixture _fixture;

        public MigrationTests(MigrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Should_Apply_All_Migrations_Successfully()
        {
            // Arrange
            await _fixture.EnsureCleanDatabaseAsync();
            using var context = _fixture.CreateContext();

            // Act
            await context.Database.MigrateAsync();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

            // Assert
            appliedMigrations.Should().NotBeEmpty();
            pendingMigrations.Should().BeEmpty();
        }

        [Fact]
        public async Task Should_Create_All_Expected_Tables()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            await context.Database.MigrateAsync();

            // Act
            var tables = await GetTableNamesAsync(context);

            // Assert
            // Core tables
            tables.Should().Contain("Users");
            tables.Should().Contain("Events");
            tables.Should().Contain("Registrations");
            tables.Should().Contain("Payments");
            tables.Should().Contain("VettingApplications");
            tables.Should().Contain("IncidentReports");
            tables.Should().Contain("RefreshTokens");
            
            // Identity tables
            tables.Should().Contain("Roles");
            tables.Should().Contain("RoleClaims");
            tables.Should().Contain("UserClaims");
            tables.Should().Contain("UserLogins");
            tables.Should().Contain("UserRoles");
            tables.Should().Contain("UserTokens");
            
            // System table
            tables.Should().Contain("__EFMigrationsHistory");
        }

        [Fact]
        public async Task Should_Have_Correct_Schema_After_Migration()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            await context.Database.MigrateAsync();

            // Act & Assert
            // Test Users table columns (Identity-based WitchCityRopeUser)
            var userColumns = await GetColumnNamesAsync(context, "Users");
            userColumns.Should().Contain("Id");
            userColumns.Should().Contain("Email");
            userColumns.Should().Contain("EmailConfirmed");
            userColumns.Should().Contain("SceneNameValue");
            userColumns.Should().Contain("PasswordHash");
            userColumns.Should().Contain("Role");
            userColumns.Should().Contain("CreatedAt");
            userColumns.Should().Contain("UpdatedAt");
            userColumns.Should().Contain("UserName");
            userColumns.Should().Contain("NormalizedUserName");
            userColumns.Should().Contain("NormalizedEmail");
            userColumns.Should().Contain("SecurityStamp");
            userColumns.Should().Contain("ConcurrencyStamp");

            // Test Events table columns
            var eventColumns = await GetColumnNamesAsync(context, "Events");
            eventColumns.Should().Contain("Id");
            eventColumns.Should().Contain("Title");
            eventColumns.Should().Contain("Description");
            eventColumns.Should().Contain("StartDateTime");
            eventColumns.Should().Contain("EndDateTime");
            eventColumns.Should().Contain("Capacity");
            eventColumns.Should().Contain("Price_Amount");
            eventColumns.Should().Contain("Price_Currency");
        }

        [Fact]
        public async Task Should_Create_Correct_Indexes()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            await context.Database.MigrateAsync();

            // Act
            var indexes = await GetIndexesAsync(context);

            // Assert
            // Check for expected indexes
            // The email index is on NormalizedEmail column in the auth schema
            indexes.Should().Contain(i => i.TableName == "Users" && i.ColumnName == "NormalizedEmail" && i.IndexName == "EmailIndex");
        }

        [Fact]
        public async Task Should_Create_Correct_Foreign_Keys()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            await context.Database.MigrateAsync();

            // Act
            var foreignKeys = await GetForeignKeysAsync(context);

            // Assert
            foreignKeys.Should().Contain(fk => 
                fk.TableName == "Registrations" && 
                fk.ColumnName == "EventId" && 
                fk.ReferencedTable == "Events");
            
            foreignKeys.Should().Contain(fk => 
                fk.TableName == "Registrations" && 
                fk.ColumnName == "UserId" && 
                fk.ReferencedTable == "Users");
        }

        [Fact]
        public void Model_Should_Match_Database_Schema()
        {
            // Arrange
            using var context = _fixture.CreateContext();

            // Act
            var model = context.Model;
            var pendingModelChanges = context.Database.HasPendingModelChanges();

            // Assert
            pendingModelChanges.Should().BeFalse("The model should match the database schema");
        }

        [Fact]
        public async Task Should_Handle_Migration_Rollback()
        {
            // This test demonstrates how you might test rollback scenarios
            // Note: This is a complex scenario and might require careful setup

            // Arrange
            using var context = _fixture.CreateContext();
            var migrator = context.Database.GetService<IMigrator>();

            // Get all migrations
            var allMigrations = context.Database.GetMigrations().ToList();
            if (allMigrations.Count < 2)
            {
                // Skip test if there aren't enough migrations to test rollback
                return;
            }

            // Apply all migrations first
            await context.Database.MigrateAsync();

            // Act - Rollback to previous migration
            var targetMigration = allMigrations[allMigrations.Count - 2];
            await migrator.MigrateAsync(targetMigration);

            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            // Assert
            appliedMigrations.Should().NotContain(allMigrations.Last());
        }

        private async Task<string[]> GetTableNamesAsync(WitchCityRopeIdentityDbContext context)
        {
            var tables = await context.Database
                .SqlQueryRaw<string>(@"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_type = 'BASE TABLE'")
                .ToArrayAsync();

            return tables;
        }

        private async Task<string[]> GetColumnNamesAsync(WitchCityRopeIdentityDbContext context, string tableName)
        {
            var columns = await context.Database
                .SqlQueryRaw<string>($@"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_schema = 'public' 
                    AND table_name = '{tableName}'")
                .ToArrayAsync();

            return columns;
        }

        private async Task<IndexInfo[]> GetIndexesAsync(WitchCityRopeIdentityDbContext context)
        {
            var indexes = await context.Database
                .SqlQueryRaw<IndexInfo>(@"
                    SELECT 
                        t.relname as TableName,
                        i.relname as IndexName,
                        a.attname as ColumnName
                    FROM pg_class t
                    JOIN pg_index ix ON t.oid = ix.indrelid
                    JOIN pg_class i ON i.oid = ix.indexrelid
                    JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
                    WHERE t.relkind = 'r'
                    AND t.relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'public')")
                .ToArrayAsync();

            return indexes;
        }

        private async Task<ForeignKeyInfo[]> GetForeignKeysAsync(WitchCityRopeIdentityDbContext context)
        {
            var foreignKeys = await context.Database
                .SqlQueryRaw<ForeignKeyInfo>(@"
                    SELECT
                        tc.table_name as TableName,
                        kcu.column_name as ColumnName,
                        ccu.table_name AS ReferencedTable,
                        ccu.column_name AS ReferencedColumn
                    FROM information_schema.table_constraints AS tc
                    JOIN information_schema.key_column_usage AS kcu
                        ON tc.constraint_name = kcu.constraint_name
                        AND tc.table_schema = kcu.table_schema
                    JOIN information_schema.constraint_column_usage AS ccu
                        ON ccu.constraint_name = tc.constraint_name
                        AND ccu.table_schema = tc.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY'
                    AND tc.table_schema = 'public'")
                .ToArrayAsync();

            return foreignKeys;
        }

        private class IndexInfo
        {
            public string TableName { get; set; } = string.Empty;
            public string IndexName { get; set; } = string.Empty;
            public string ColumnName { get; set; } = string.Empty;
        }

        private class ForeignKeyInfo
        {
            public string TableName { get; set; } = string.Empty;
            public string ColumnName { get; set; } = string.Empty;
            public string ReferencedTable { get; set; } = string.Empty;
            public string ReferencedColumn { get; set; } = string.Empty;
        }
    }
}