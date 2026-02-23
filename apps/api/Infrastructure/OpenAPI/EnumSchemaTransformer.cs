using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
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
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        // Update or add VettingStatus enum schema
        if (document.Components.Schemas.TryGetValue("VettingStatus", out var existingSchema)
            && existingSchema is OpenApiSchema concreteSchema)
        {
            // Schema already exists, just ensure it has enum values
            if (concreteSchema.Enum == null || concreteSchema.Enum.Count == 0)
            {
                concreteSchema.Type = JsonSchemaType.String;
                concreteSchema.Enum = Enum.GetNames(typeof(VettingStatus))
                    .Select(name => (JsonNode)JsonValue.Create(name)!)
                    .ToList();
                concreteSchema.Description = "Vetting application workflow status";
            }
        }
        else
        {
            // Schema doesn't exist, create it
            var vettingStatusSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = Enum.GetNames(typeof(VettingStatus))
                    .Select(name => (JsonNode)JsonValue.Create(name)!)
                    .ToList(),
                Description = "Vetting application workflow status"
            };

            document.Components.Schemas["VettingStatus"] = vettingStatusSchema;
        }

        return Task.CompletedTask;
    }
}
