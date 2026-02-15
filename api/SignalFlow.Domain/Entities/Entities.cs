using SignalFlow.Domain.Enums;

namespace SignalFlow.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string ApiKeyHash { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public TenantConfig Config { get; set; } = null!;
}

public sealed class TenantConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string DefaultModel { get; set; } = "fake-model-v1";
    public bool RequireSchemaValid { get; set; } = true;
    public bool AllowHumanOverride { get; set; } = true;

    // Growth-lender defaults
    public decimal MinConfidence { get; set; } = 0.60m;
    public int HardMinCreditScore { get; set; } = 580;
    public int ReviewCreditScoreBelow { get; set; } = 620;
    public decimal ReviewDTIAbove { get; set; } = 0.43m;
    public decimal HardMaxDTI { get; set; } = 0.55m;

    public int MaxDelinquencies12m { get; set; } = 2;
    public int MaxRequestedAmount { get; set; } = 25000;

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PromptTemplate
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public List<PromptTemplateVersion> Versions { get; set; } = new();
}

public sealed class PromptTemplateVersion
{
    public Guid Id { get; set; }
    public Guid PromptTemplateId { get; set; }

    public int Version { get; set; }
    public TemplateStatus Status { get; set; }

    public string Content { get; set; } = null!;
    public string OutputSchemaJson { get; set; } = null!;
    public string? InputSchemaJson { get; set; }

    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DecisionRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TemplateVersionId { get; set; }

    public RunType RunType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string InputJson { get; set; } = null!;
    public string RenderedPrompt { get; set; } = null!;

    public string Model { get; set; } = null!;
    public int LatencyMs { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }

    public string AiOutputJson { get; set; } = null!;
    public bool SchemaValid { get; set; }
    public string? SchemaErrorsJson { get; set; }

    public DecisionType FinalDecision { get; set; }
    public string DecisionReason { get; set; } = null!;
    public bool PolicyPassed { get; set; }

    public string AuditHash { get; set; } = null!;

    public List<PolicyEvaluation> PolicyEvaluations { get; set; } = new();
    public HumanOverride? HumanOverride { get; set; }
}

public sealed class PolicyEvaluation
{
    public Guid Id { get; set; }
    public Guid DecisionRunId { get; set; }

    public string PolicyKey { get; set; } = null!;
    public bool Passed { get; set; }
    public string DetailsJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class HumanOverride
{
    public Guid Id { get; set; }
    public Guid DecisionRunId { get; set; }

    public string OverriddenBy { get; set; } = null!;
    public DateTimeOffset OverrideAt { get; set; }

    public DecisionType NewDecision { get; set; }
    public string Reason { get; set; } = null!;
    public string? Notes { get; set; }
}
