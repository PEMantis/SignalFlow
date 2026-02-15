namespace SignalFlow.Application.Services;

public sealed record ModelRequest(string RenderedPrompt, string Model, string InputJson);
public sealed record ModelUsage(int PromptTokens, int CompletionTokens, int TotalTokens);
public sealed record ModelResponse(string OutputJson, int LatencyMs, ModelUsage Usage, string Model);

public interface IModelClient
{
    Task<ModelResponse> GenerateAsync(ModelRequest request, CancellationToken ct);
}
