using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Infrastructure.OpenAPI;

/// <summary>
/// OpenAPI document transformer that properly exposes enum types with their values
/// Ensures enums are documented in OpenAPI spec with all possible values
/// </summary>
public class EnumSchemaTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Ensure components exist
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        // Update or add VettingStatus enum schema
        if (document.Components.Schemas.TryGetValue("VettingStatus", out var existingSchema))
        {
            // Schema already exists, just ensure it has enum values
            if (existingSchema.Enum == null || existingSchema.Enum.Count == 0)
            {
                existingSchema.Type = "string";
                existingSchema.Enum = Enum.GetNames(typeof(VettingStatus))
                    .Select(name => new OpenApiString(name) as IOpenApiAny)
                    .ToList();
                existingSchema.Description = "Vetting application workflow status";
            }
        }
        else
        {
            // Schema doesn't exist, create it
            var vettingStatusSchema = new OpenApiSchema
            {
                Type = "string",
                Enum = Enum.GetNames(typeof(VettingStatus))
                    .Select(name => new OpenApiString(name) as IOpenApiAny)
                    .ToList(),
                Description = "Vetting application workflow status"
            };

            document.Components.Schemas["VettingStatus"] = vettingStatusSchema;
        }

        return Task.CompletedTask;
    }

    private void UpdateSchemaEnumReferences(OpenApiSchema schema)
    {
        if (schema.Properties != null)
        {
            foreach (var (propertyName, propertySchema) in schema.Properties)
            {
                // If property is named "status" or "vettingStatus" and is a string, convert to enum reference
                if ((propertyName.Equals("status", StringComparison.OrdinalIgnoreCase) ||
                     propertyName.Equals("vettingStatus", StringComparison.OrdinalIgnoreCase)) &&
                    propertySchema.Type == "string" &&
                    propertySchema.Enum == null)
                {
                    // Replace with reference to VettingStatus enum schema
                    propertySchema.Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = "VettingStatus"
                    };
                    propertySchema.Type = null; // Clear type when using reference
                }

                // Recursively process nested schemas
                if (propertySchema.Properties != null)
                {
                    UpdateSchemaEnumReferences(propertySchema);
                }
            }
        }
    }
}
