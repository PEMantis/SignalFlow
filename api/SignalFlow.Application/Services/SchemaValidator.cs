using NJsonSchema;

namespace SignalFlow.Application.Services;

public sealed record SchemaValidationResult(bool Valid, string[] Errors);

public sealed class SchemaValidator
{
    public async Task<SchemaValidationResult> ValidateAsync(string schemaJson, string json)
    {
        var schema = await JsonSchema.FromJsonAsync(schemaJson);
        var errors = schema.Validate(json);
        if (errors.Count == 0) return new SchemaValidationResult(true, Array.Empty<string>());

        return new SchemaValidationResult(
            false,
            errors.Select(e => $"{e.Path}: {e.Kind}").Distinct().ToArray()
        );
    }
}
