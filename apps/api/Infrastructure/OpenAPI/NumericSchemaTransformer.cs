using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WitchCityRope.Api.Infrastructure.OpenAPI;

/// <summary>
/// OpenAPI schema transformer that fixes numeric type generation for TypeScript clients.
///
/// PROBLEM: ASP.NET Core's Microsoft.AspNetCore.OpenApi generates integer and number
/// schemas as "type": ["integer", "string"] (or ["number", "string"]) because the default
/// JsonSerializerOptions.NumberHandling is AllowReadingFromString. This causes
/// openapi-typescript to generate "number | string" union types instead of just "number",
/// breaking arithmetic operations and comparisons in TypeScript code.
///
/// FIX: This transformer strips the String flag from any schema that is primarily
/// Integer or Number, ensuring the generated TypeScript types are clean "number" types.
/// Also clears the regex pattern that .NET adds for string-encoded number validation.
///
/// REFERENCES:
/// - dotnet/aspnetcore#61038: integers have additional string type with pattern
/// - dotnet/aspnetcore#64145: Integers represented as both integer/string
/// - dotnet/aspnetcore#64920: OpenApi default number types
/// - John Reilly: "Full-stack static typing with OpenAPI TypeScript and Microsoft.AspNetCore.OpenApi"
/// </summary>
public sealed class NumericSchemaTransformer : IOpenApiSchemaTransformer
{
    // The set of .NET types that should produce clean integer schemas (no string union)
    private static readonly HashSet<Type> IntegerTypes = new()
    {
        typeof(int),
        typeof(long),
        typeof(short),
        typeof(byte),
        typeof(sbyte),
        typeof(uint),
        typeof(ulong),
        typeof(ushort)
    };

    // The set of .NET types that should produce clean number schemas (no string union)
    private static readonly HashSet<Type> NumberTypes = new()
    {
        typeof(decimal),
        typeof(double),
        typeof(float)
    };

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Get the actual CLR type, unwrapping Nullable<T> if present
        var clrType = context.JsonTypeInfo.Type;
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        // schema.Type is nullable; skip if not set
        if (schema.Type is not { } schemaType)
        {
            return Task.CompletedTask;
        }

        if (IntegerTypes.Contains(underlyingType))
        {
            // Force schema to pure integer type, removing String flag
            // Preserve Null flag if the original type was nullable
            var isNullable = schemaType.HasFlag(JsonSchemaType.Null);
            schema.Type = isNullable
                ? JsonSchemaType.Integer | JsonSchemaType.Null
                : JsonSchemaType.Integer;

            // Clear the regex pattern that .NET adds for string-encoded number validation
            // (e.g., "^-?[0-9]+$") since we no longer accept string input
            schema.Pattern = null;

            // Preserve the format (.NET sets "int32" or "int64" correctly)
            // schema.Format is already set correctly by the framework
        }
        else if (NumberTypes.Contains(underlyingType))
        {
            // Force schema to pure number type, removing String flag
            var isNullable = schemaType.HasFlag(JsonSchemaType.Null);
            schema.Type = isNullable
                ? JsonSchemaType.Number | JsonSchemaType.Null
                : JsonSchemaType.Number;

            // Clear any string-related pattern
            schema.Pattern = null;

            // Preserve format: "double" for double/float, unset for decimal
            // The framework already sets this appropriately
        }

        return Task.CompletedTask;
    }
}
