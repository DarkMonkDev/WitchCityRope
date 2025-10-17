using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace WitchCityRope.Api.Infrastructure.OpenAPI;

/// <summary>
/// OpenAPI document transformer that adds JWT Bearer token authentication scheme
/// Supports both Authorization header and httpOnly cookie authentication
/// </summary>
public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Define Bearer authentication scheme
        var bearerScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = "Authorization",
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. " +
                         "Enter 'Bearer' [space] and then your token in the text input below. " +
                         "Example: 'Bearer 12345abcdef'. " +
                         "Note: Cookie-based authentication (auth-token) is also supported for BFF pattern."
        };

        // Initialize components if needed
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        // Add Bearer scheme to components
        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        // Apply Bearer security requirement globally to all endpoints
        var securityRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        };

        document.SecurityRequirements ??= new List<OpenApiSecurityRequirement>();
        document.SecurityRequirements.Add(securityRequirement);

        return Task.CompletedTask;
    }
}
