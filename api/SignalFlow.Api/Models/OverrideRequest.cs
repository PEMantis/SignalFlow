namespace SignalFlow.Api.Models;

public sealed class OverrideRequest
{
    public string NewDecision { get; set; } = null!; // "Approve" | "Deny" | "Review"
    public string Reason { get; set; } = null!;
    public string? Notes { get; set; }
    public string OverriddenBy { get; set; } = "admin";
}
