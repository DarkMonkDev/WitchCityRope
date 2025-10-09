using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Base class for DTO mapping validation tests.
/// Provides generic test methods to verify DTO properties correctly map to Entity properties.
/// CRITICAL: These tests catch DTO/Entity mismatches that cause bugs like the profile update issue.
/// </summary>
public abstract class DtoMappingTestBase : IntegrationTestBase
{
    protected readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Factory;

    protected DtoMappingTestBase(DatabaseTestFixture fixture) : base(fixture)
    {
        // Create web application factory for API testing
        Factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database with test database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    });
                });
            });
    }

    /// <summary>
    /// Verifies that all properties in a DTO exist on the corresponding Entity.
    /// CRITICAL: Catches cases where DTO has fields that can't be saved to database.
    /// </summary>
    /// <typeparam name="TDto">DTO type to validate</typeparam>
    /// <typeparam name="TEntity">Entity type to validate against</typeparam>
    /// <param name="excludedProperties">Properties to skip (e.g., computed fields, DTOs with different naming)</param>
    protected void AssertDtoPropertiesExistOnEntity<TDto, TEntity>(params string[] excludedProperties)
    {
        // Get all public properties from DTO
        var dtoProperties = typeof(TDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !excludedProperties.Contains(p.Name))
            .ToList();

        // Get all public properties from Entity
        var entityProperties = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var missingProperties = new List<string>();

        foreach (var dtoProperty in dtoProperties)
        {
            if (!entityProperties.ContainsKey(dtoProperty.Name))
            {
                missingProperties.Add(dtoProperty.Name);
            }
        }

        // CRITICAL: If properties are missing, this is a BUG that will cause data loss
        missingProperties.Should().BeEmpty(
            $"DTO {typeof(TDto).Name} has properties that don't exist on Entity {typeof(TEntity).Name}. " +
            $"This causes data loss or errors when saving to database. Missing properties: {string.Join(", ", missingProperties)}");
    }

    /// <summary>
    /// Verifies that DTO property types are compatible with Entity property types.
    /// Catches type mismatches that cause serialization or mapping errors.
    /// </summary>
    protected void AssertDtoPropertyTypesCompatible<TDto, TEntity>(params string[] excludedProperties)
    {
        var dtoProperties = typeof(TDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !excludedProperties.Contains(p.Name))
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var entityProperties = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var incompatibleProperties = new List<string>();

        foreach (var dtoProperty in dtoProperties.Values)
        {
            if (entityProperties.TryGetValue(dtoProperty.Name, out var entityProperty))
            {
                // Check if types are compatible (exact match or nullable match)
                var dtoType = Nullable.GetUnderlyingType(dtoProperty.PropertyType) ?? dtoProperty.PropertyType;
                var entityType = Nullable.GetUnderlyingType(entityProperty.PropertyType) ?? entityProperty.PropertyType;

                if (dtoType != entityType && !AreTypesCompatible(dtoType, entityType))
                {
                    incompatibleProperties.Add(
                        $"{dtoProperty.Name}: DTO={dtoProperty.PropertyType.Name}, Entity={entityProperty.PropertyType.Name}");
                }
            }
        }

        incompatibleProperties.Should().BeEmpty(
            $"DTO {typeof(TDto).Name} has properties with incompatible types compared to Entity {typeof(TEntity).Name}. " +
            $"This causes mapping or serialization errors. Incompatible: {string.Join(", ", incompatibleProperties)}");
    }

    /// <summary>
    /// Verifies that data saves correctly to database when mapping DTO to Entity.
    /// This is an end-to-end integration test with real database.
    /// </summary>
    protected async Task<TEntity> AssertDtoSavesToDatabase<TDto, TEntity>(
        TDto dto,
        Func<TDto, TEntity> mapToEntity,
        Func<ApplicationDbContext, DbSet<TEntity>> getDbSet)
        where TEntity : class
    {
        // Arrange: Map DTO to Entity
        var entity = mapToEntity(dto);

        // Act: Save to database
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var dbSet = getDbSet(context);
        dbSet.Add(entity);
        await context.SaveChangesAsync();

        // Get the primary key property (assume it's named "Id")
        var idProperty = typeof(TEntity).GetProperty("Id");
        idProperty.Should().NotBeNull("Entity must have an Id property");

        var entityId = idProperty!.GetValue(entity);
        entityId.Should().NotBeNull("Entity Id should be set after save");

        // Assert: Verify entity was saved and can be retrieved
        context.Entry(entity).State = EntityState.Detached; // Detach to force fresh load

        var savedEntity = await dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<object>(e, "Id")!.Equals(entityId));

        savedEntity.Should().NotBeNull(
            $"Entity {typeof(TEntity).Name} should be retrievable from database after save");

        return savedEntity!;
    }

    /// <summary>
    /// Verifies that all DTO fields are returned correctly from API endpoint.
    /// Catches cases where endpoint returns entity instead of DTO or forgets to map fields.
    /// </summary>
    protected async Task AssertApiReturnsAllDtoFields<TDto>(
        string endpoint,
        Func<TDto, bool> validateDto)
    {
        // Arrange: Make authenticated request
        var client = CreateAuthenticatedClient();

        // Act: Call API endpoint
        var response = await client.GetAsync(endpoint);

        // Assert: Response successful
        response.IsSuccessStatusCode.Should().BeTrue($"API endpoint {endpoint} should return success");

        // Assert: Response deserializes to DTO
        var dto = await response.Content.ReadFromJsonAsync<TDto>();
        dto.Should().NotBeNull($"API endpoint {endpoint} should return {typeof(TDto).Name}");

        // Assert: DTO has expected data
        var isValid = validateDto(dto!);
        isValid.Should().BeTrue($"API endpoint {endpoint} returned DTO with missing or invalid fields");
    }

    /// <summary>
    /// Checks if two types are compatible for DTO/Entity mapping.
    /// Handles common conversions like string to enum, int to Guid, etc.
    /// </summary>
    private bool AreTypesCompatible(Type dtoType, Type entityType)
    {
        // Allow string DTOs for enum entities (common pattern)
        if (dtoType == typeof(string) && entityType.IsEnum)
            return true;

        // Allow int DTOs for enum entities
        if (dtoType == typeof(int) && entityType.IsEnum)
            return true;

        // Allow Guid DTOs for string entities (sometimes used for external IDs)
        if (dtoType == typeof(Guid) && entityType == typeof(string))
            return true;

        // Allow string DTOs for Guid entities
        if (dtoType == typeof(string) && entityType == typeof(Guid))
            return true;

        // Allow decimal/double conversions
        if ((dtoType == typeof(decimal) && entityType == typeof(double)) ||
            (dtoType == typeof(double) && entityType == typeof(decimal)))
            return true;

        return false;
    }

    /// <summary>
    /// Creates an HTTP client with authentication headers for API testing
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string? userId = null, string? email = null, string? role = null)
    {
        var client = Factory.CreateClient();

        // Generate JWT token for authentication
        var userIdGuid = string.IsNullOrEmpty(userId) ? Guid.NewGuid() : Guid.Parse(userId);
        var userEmail = email ?? $"test_{Guid.NewGuid():N}@example.com";
        var userRole = role ?? "Member";

        var token = GenerateJwtToken(userIdGuid, userEmail, userRole);

        // Add auth header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
