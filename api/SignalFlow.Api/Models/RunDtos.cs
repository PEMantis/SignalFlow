namespace SignalFlow.Api.Models;

public sealed class CreateRunRequest
{
    public string TemplateKey { get; set; } = "credit_risk_prescreen";
    public object Input { get; set; } = null!;
}
