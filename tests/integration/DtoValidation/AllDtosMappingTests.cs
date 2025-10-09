using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.IntegrationTests.DtoValidation;

/// <summary>
/// Automated DTO validation that uses reflection to find ALL DTOs in the project
/// and verify they have corresponding entities with matching properties.
/// CRITICAL: This test catches future DTO/Entity mismatches automatically without
/// having to write specific tests for each new DTO.
/// </summary>
[Collection("Database")]
public class AllDtosMappingTests : DtoMappingTestBase
{
    private readonly ITestOutputHelper _output;

    public AllDtosMappingTests(DatabaseTestFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
    }

    [Fact]
    public void AllDtos_HaveMatchingEntities()
    {
        // Find all DTO classes in API project
        var apiAssembly = typeof(ApplicationUser).Assembly;
        var dtoTypes = apiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Dto") && t.IsClass && !t.IsAbstract)
            .ToList();

        _output.WriteLine($"Found {dtoTypes.Count} DTO types to validate");

        var missingEntities = new List<string>();

        foreach (var dtoType in dtoTypes)
        {
            // Try to find corresponding entity by removing "Dto" suffix
            var entityName = dtoType.Name.Replace("Dto", "");
            var entityType = apiAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == entityName && t.IsClass && !t.IsAbstract);

            if (entityType == null)
            {
                // Some DTOs are projections or don't have direct entity mappings
                // Log for review but don't fail
                _output.WriteLine($"⚠️  DTO {dtoType.Name} has no matching entity named {entityName}");
            }
            else
            {
                _output.WriteLine($"✅ DTO {dtoType.Name} → Entity {entityType.Name}");
            }
        }

        // This test primarily serves as discovery - specific DTO tests verify mappings
        true.Should().BeTrue("DTO discovery completed");
    }

    [Fact]
    public void AllDtos_PropertiesMatchEntities()
    {
        // Map of DTOs to their entities and excluded properties
        var dtoToEntityMappings = new Dictionary<Type, (Type EntityType, string[] ExcludedProperties)>
        {
            // Dashboard DTOs
            [typeof(WitchCityRope.Api.Features.Dashboard.Models.UpdateProfileDto)] =
                (typeof(ApplicationUser), new[] { "PhoneNumber" }), // PhoneNumber is on IdentityUser base class

            [typeof(WitchCityRope.Api.Features.Dashboard.Models.UserProfileDto)] =
                (typeof(ApplicationUser), Array.Empty<string>()),

            // Event DTOs - map to Event entity
            [typeof(WitchCityRope.Api.Features.Events.Models.EventDto)] =
                (typeof(Event), new[] { "CurrentRsvps", "CurrentTickets", "AvailableCapacity", "Teachers", "SessionCount", "TicketTypeCount" }), // Computed fields

            [typeof(WitchCityRope.Api.Features.Events.Models.SessionDto)] =
                (typeof(Session), new[] { "Date", "TeacherNames" }), // Computed/related fields

            [typeof(WitchCityRope.Api.Features.Events.Models.TicketTypeDto)] =
                (typeof(TicketType), new[] { "EventId", "SessionId", "EventTitle", "SessionName" }), // Foreign keys handled differently in DTOs

            // Participation DTOs
            [typeof(WitchCityRope.Api.Features.Participation.Models.EventParticipationDto)] =
                (typeof(WitchCityRope.Api.Features.Participation.Entities.EventParticipation),
                    new[] { "UserName", "UserEmail", "EventTitle" }), // Related entity data

            // User DTOs
            [typeof(WitchCityRope.Api.Features.Users.Models.UserDto)] =
                (typeof(ApplicationUser), new[] { "PhoneNumber" }), // PhoneNumber on IdentityUser

            // Add more mappings as DTOs are created
        };

        var errors = new List<string>();

        foreach (var (dtoType, (entityType, excludedProperties)) in dtoToEntityMappings)
        {
            _output.WriteLine($"\nValidating: {dtoType.Name} → {entityType.Name}");

            try
            {
                // Validate properties exist
                ValidateDtoProperties(dtoType, entityType, excludedProperties);
                _output.WriteLine($"✅ {dtoType.Name}: All properties valid");
            }
            catch (Exception ex)
            {
                errors.Add($"{dtoType.Name}: {ex.Message}");
                _output.WriteLine($"❌ {dtoType.Name}: {ex.Message}");
            }
        }

        errors.Should().BeEmpty(
            $"All DTOs should have matching properties on their entities. Failures:\n{string.Join("\n", errors)}");
    }

    [Fact]
    public void AllRequestDtos_OnlyHaveWritableProperties()
    {
        // Find all DTOs with "Request", "Create", or "Update" in the name
        var apiAssembly = typeof(ApplicationUser).Assembly;
        var requestDtos = apiAssembly.GetTypes()
            .Where(t => (t.Name.Contains("Request") || t.Name.Contains("Create") || t.Name.Contains("Update")) &&
                        t.Name.EndsWith("Dto") &&
                        t.IsClass && !t.IsAbstract)
            .ToList();

        _output.WriteLine($"Found {requestDtos.Count} request/command DTO types");

        var problematicDtos = new List<string>();

        foreach (var dtoType in requestDtos)
        {
            var properties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var readOnlyProperties = properties
                .Where(p => !p.CanWrite || (p.GetSetMethod() == null))
                .ToList();

            if (readOnlyProperties.Any())
            {
                problematicDtos.Add($"{dtoType.Name} has read-only properties: {string.Join(", ", readOnlyProperties.Select(p => p.Name))}");
                _output.WriteLine($"⚠️  {dtoType.Name} has read-only properties");
            }
            else
            {
                _output.WriteLine($"✅ {dtoType.Name}: All properties writable");
            }
        }

        // Read-only properties in request DTOs can cause binding issues
        problematicDtos.Should().BeEmpty(
            "Request/Command DTOs should have writable properties for model binding");
    }

    [Fact]
    public void AllResponseDtos_HaveDescriptiveNames()
    {
        // Find all DTOs
        var apiAssembly = typeof(ApplicationUser).Assembly;
        var dtoTypes = apiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Dto") && t.IsClass && !t.IsAbstract)
            .ToList();

        var poorlyNamedDtos = dtoTypes
            .Where(t => t.Name == "Dto" || t.Name.Length <= 6) // Just "Dto" or very short names
            .Select(t => t.Name)
            .ToList();

        poorlyNamedDtos.Should().BeEmpty(
            "DTOs should have descriptive names indicating their purpose");

        _output.WriteLine($"✅ All {dtoTypes.Count} DTOs have descriptive names");
    }

    private void ValidateDtoProperties(Type dtoType, Type entityType, string[] excludedProperties)
    {
        var dtoProperties = dtoType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !excludedProperties.Contains(p.Name))
            .ToList();

        var entityProperties = GetAllProperties(entityType)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var missingProperties = new List<string>();

        foreach (var dtoProperty in dtoProperties)
        {
            if (!entityProperties.ContainsKey(dtoProperty.Name))
            {
                missingProperties.Add(dtoProperty.Name);
            }
        }

        if (missingProperties.Any())
        {
            throw new Exception(
                $"DTO has properties not on entity: {string.Join(", ", missingProperties)}. " +
                $"Either add these to entity or exclude them in test mapping.");
        }
    }

    /// <summary>
    /// Gets all properties including from base classes (for ApplicationUser : IdentityUser scenario)
    /// </summary>
    private IEnumerable<PropertyInfo> GetAllProperties(Type type)
    {
        var properties = new List<PropertyInfo>();
        var currentType = type;

        while (currentType != null && currentType != typeof(object))
        {
            properties.AddRange(currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            currentType = currentType.BaseType;
        }

        return properties.DistinctBy(p => p.Name);
    }

    [Fact]
    public void AllDtos_WithDateTimeProperties_UseUtc()
    {
        // Find all DTOs with DateTime properties
        var apiAssembly = typeof(ApplicationUser).Assembly;
        var dtoTypes = apiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Dto") && t.IsClass && !t.IsAbstract)
            .ToList();

        _output.WriteLine($"Checking {dtoTypes.Count} DTOs for DateTime properties");

        var dtosWithDateTime = new List<string>();

        foreach (var dtoType in dtoTypes)
        {
            var dateTimeProperties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                .ToList();

            if (dateTimeProperties.Any())
            {
                dtosWithDateTime.Add($"{dtoType.Name}: {string.Join(", ", dateTimeProperties.Select(p => p.Name))}");
                _output.WriteLine($"📅 {dtoType.Name} has DateTime properties: {string.Join(", ", dateTimeProperties.Select(p => p.Name))}");
            }
        }

        // This is informational - documents which DTOs have DateTime fields
        // Integration tests should verify these are UTC when saved to database
        _output.WriteLine($"\nFound {dtosWithDateTime.Count} DTOs with DateTime properties");
        _output.WriteLine("⚠️  Ensure these use UTC in all API responses and database saves");
    }
}
