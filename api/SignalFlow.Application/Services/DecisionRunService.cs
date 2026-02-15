using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SignalFlow.Application.Policies;
using SignalFlow.Domain.Entities;
using SignalFlow.Domain.Enums;
using SignalFlow.Infrastructure.Data;

namespace SignalFlow.Application.Services;

public sealed class DecisionRunService
{
    private readonly SignalFlowDbContext _db;
    private readonly IModelClient _model;
    private readonly SchemaValidator _schema;

    public DecisionRunService(SignalFlowDbContext db, IModelClient model, SchemaValidator schema)
    {
        _db = db;
        _model = model;
        _schema = schema;
    }

    public async Task<DecisionRun> RunAsync(Guid tenantId, TenantConfig cfg, string templateKey, string inputJson, CancellationToken ct)
    {
        var template = await _db.PromptTemplates.FirstAsync(t => t.TenantId == tenantId && t.Key == templateKey, ct);
        var version = await _db.PromptTemplateVersions
            .Where(v => v.PromptTemplateId == template.Id && v.Status == TemplateStatus.Published)
            .OrderByDescending(v => v.Version)
            .FirstAsync(ct);

        var renderedPrompt = version.Content
            .Replace("{{OutputSchemaJson}}", version.OutputSchemaJson)
            .Replace("{{InputJson}}", inputJson);

        var modelResp = await _model.GenerateAsync(new ModelRequest(renderedPrompt, cfg.DefaultModel, inputJson), ct);

        var schemaResult = await _schema.ValidateAsync(version.OutputSchemaJson, modelResp.OutputJson);

        var policies = CreditRiskPolicies.Evaluate(cfg, inputJson, modelResp.OutputJson, schemaResult.Valid);
        var (decision, reason, passed) = CreditRiskPolicies.Resolve(policies);

        var run = new DecisionRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateVersionId = version.Id,
            RunType = RunType.Live,
            CreatedAt = DateTimeOffset.UtcNow,

            InputJson = inputJson,
            RenderedPrompt = renderedPrompt,

            Model = modelResp.Model,
            LatencyMs = modelResp.LatencyMs,
            PromptTokens = modelResp.Usage.PromptTokens,
            CompletionTokens = modelResp.Usage.CompletionTokens,
            TotalTokens = modelResp.Usage.TotalTokens,

            AiOutputJson = modelResp.OutputJson,
            SchemaValid = schemaResult.Valid,
            SchemaErrorsJson = schemaResult.Valid ? null : JsonSerializer.Serialize(schemaResult.Errors),

            FinalDecision = decision,
            DecisionReason = reason,
            PolicyPassed = passed,
            AuditHash = "" // set below
        };

        run.PolicyEvaluations = policies.Select(p => new PolicyEvaluation
        {
            Id = Guid.NewGuid(),
            DecisionRunId = run.Id,
            PolicyKey = p.PolicyKey,
            Passed = p.Passed,
            DetailsJson = p.DetailsJson,
            CreatedAt = DateTimeOffset.UtcNow
        }).ToList();

        run.AuditHash = ComputeAuditHash(run);

        _db.DecisionRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        return run;
    }

    public async Task<DecisionRun> ReplayAsync(Guid tenantId, TenantConfig cfg, Guid runId, CancellationToken ct)
    {
        var original = await _db.DecisionRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId && r.TenantId == tenantId, ct);

        if (original is null)
            throw new InvalidOperationException("Run not found.");

        // Need template version to validate schema
        var version = await _db.PromptTemplateVersions
            .AsNoTracking()
            .FirstAsync(v => v.Id == original.TemplateVersionId, ct);

        var schemaResult = await _schema.ValidateAsync(version.OutputSchemaJson, original.AiOutputJson);

        var policies = CreditRiskPolicies.Evaluate(cfg, original.InputJson, original.AiOutputJson, schemaResult.Valid);
        var (decision, reason, passed) = CreditRiskPolicies.Resolve(policies);

        var replay = new DecisionRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateVersionId = original.TemplateVersionId,
            RunType = RunType.Replay,
            CreatedAt = DateTimeOffset.UtcNow,

            // Replay uses stored artifacts
            InputJson = original.InputJson,
            RenderedPrompt = original.RenderedPrompt,

            Model = original.Model,
            LatencyMs = 0, // no model call
            PromptTokens = 0,
            CompletionTokens = 0,
            TotalTokens = 0,

            AiOutputJson = original.AiOutputJson,
            SchemaValid = schemaResult.Valid,
            SchemaErrorsJson = schemaResult.Valid ? null : System.Text.Json.JsonSerializer.Serialize(schemaResult.Errors),

            FinalDecision = decision,
            DecisionReason = $"Replay:{reason}",
            PolicyPassed = passed,
            AuditHash = ""
        };

        replay.PolicyEvaluations = policies.Select(p => new PolicyEvaluation
        {
            Id = Guid.NewGuid(),
            DecisionRunId = replay.Id,
            PolicyKey = p.PolicyKey,
            Passed = p.Passed,
            DetailsJson = p.DetailsJson,
            CreatedAt = DateTimeOffset.UtcNow
        }).ToList();

        replay.AuditHash = ComputeAuditHash(replay);

        _db.DecisionRuns.Add(replay);
        await _db.SaveChangesAsync(ct);

        return replay;
    }
    private static string ComputeAuditHash(DecisionRun run)
    {
        // Keep it stable: hash a canonical string of key fields
        var canonical = string.Join("|", new[]
        {
            run.Id.ToString(),
            run.TenantId.ToString(),
            run.TemplateVersionId.ToString(),
            run.CreatedAt.ToUnixTimeMilliseconds().ToString(),
            run.Model,
            run.LatencyMs.ToString(),
            run.PromptTokens.ToString(),
            run.CompletionTokens.ToString(),
            run.TotalTokens.ToString(),
            run.SchemaValid.ToString(),
            run.FinalDecision.ToString(),
            run.DecisionReason,
            run.InputJson,
            run.AiOutputJson
        });

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
