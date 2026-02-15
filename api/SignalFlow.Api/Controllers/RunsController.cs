using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalFlow.Api.Middleware;
using SignalFlow.Api.Models;
using SignalFlow.Application.Services;
using SignalFlow.Domain.Entities;
using SignalFlow.Domain.Enums;
using SignalFlow.Infrastructure.Data;

namespace SignalFlow.Api.Controllers;

[ApiController]
[Route("api/runs")]
public sealed class RunsController : ControllerBase
{
    private readonly DecisionRunService _runs;
    private readonly SignalFlowDbContext _db;

    public RunsController(DecisionRunService runs, SignalFlowDbContext db)
    {
        _runs = runs;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRunRequest req, CancellationToken ct)
    {
        var tenantId = HttpContext.TenantId();
        var cfg = HttpContext.TenantConfig();

        var inputJson = JsonSerializer.Serialize(req.Input, new JsonSerializerOptions { WriteIndented = false });

        var run = await _runs.RunAsync(tenantId, cfg, req.TemplateKey, inputJson, ct);

        return Ok(new
        {
            runId = run.Id,
            runType = run.RunType.ToString(),
            createdAt = run.CreatedAt,
            templateVersionId = run.TemplateVersionId,
            model = run.Model,
            latencyMs = run.LatencyMs,
            tokens = new { prompt = run.PromptTokens, completion = run.CompletionTokens, total = run.TotalTokens },
            schema = new { valid = run.SchemaValid, errors = run.SchemaErrorsJson is null ? Array.Empty<string>() : JsonSerializer.Deserialize<string[]>(run.SchemaErrorsJson) },
            input = JsonDocument.Parse(run.InputJson).RootElement,
            aiOutput = JsonDocument.Parse(run.AiOutputJson).RootElement,
            policyChecks = run.PolicyEvaluations.Select(p => new { p.PolicyKey, p.Passed, details = JsonDocument.Parse(p.DetailsJson).RootElement }),
            finalDecision = run.FinalDecision.ToString(),
            decisionReason = run.DecisionReason,
            auditHash = run.AuditHash
        });
    }

    [HttpGet("{runId:guid}")]
    public async Task<IActionResult> Get(Guid runId, CancellationToken ct)
    {
        var tenantId = HttpContext.TenantId();

        var run = await _db.DecisionRuns
            .Include(r => r.PolicyEvaluations)
            .Include(r => r.HumanOverride)
            .FirstOrDefaultAsync(r => r.Id == runId && r.TenantId == tenantId, ct);

        if (run is null) return NotFound();

        return Ok(run);
    }

    [HttpPost("{runId:guid}/replay")]
    public async Task<IActionResult> Replay(Guid runId, CancellationToken ct)
    {
        var tenantId = HttpContext.TenantId();
        var cfg = HttpContext.TenantConfig();

        var run = await _runs.ReplayAsync(tenantId, cfg, runId, ct);

        return Ok(new
        {
            runId = run.Id,
            runType = run.RunType.ToString(),
            createdAt = run.CreatedAt,
            templateVersionId = run.TemplateVersionId,
            model = run.Model,
            latencyMs = run.LatencyMs,
            tokens = new { prompt = run.PromptTokens, completion = run.CompletionTokens, total = run.TotalTokens },
            schema = new
            {
                valid = run.SchemaValid,
                errors = run.SchemaErrorsJson is null
                    ? Array.Empty<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<string[]>(run.SchemaErrorsJson)
            },
            input = System.Text.Json.JsonDocument.Parse(run.InputJson).RootElement,
            aiOutput = System.Text.Json.JsonDocument.Parse(run.AiOutputJson).RootElement,
            policyChecks = run.PolicyEvaluations.Select(p => new
            {
                policyKey = p.PolicyKey,
                passed = p.Passed,
                details = System.Text.Json.JsonDocument.Parse(p.DetailsJson).RootElement
            }),
            finalDecision = run.FinalDecision.ToString(),
            decisionReason = run.DecisionReason,
            auditHash = run.AuditHash
        });
    }

    [HttpPost("{runId:guid}/override")]
    public async Task<IActionResult> Override(Guid runId, [FromBody] OverrideRequest req, CancellationToken ct)
    {
        var tenantId = HttpContext.TenantId();
        var cfg = HttpContext.TenantConfig();

        if (!cfg.AllowHumanOverride)
            return Forbid("Human override disabled for this tenant.");

        var run = await _db.DecisionRuns
            .Include(r => r.HumanOverride)
            .FirstOrDefaultAsync(r => r.Id == runId && r.TenantId == tenantId, ct);

        if (run is null) return NotFound();

        if (run.HumanOverride is not null)
            return Conflict("Run already has an override.");

        if (!Enum.TryParse<DecisionType>(req.NewDecision, ignoreCase: true, out var decision))
            return BadRequest("NewDecision must be Approve, Deny, or Review.");

        var ov = new HumanOverride
        {
            Id = Guid.NewGuid(),
            DecisionRunId = run.Id,
            OverriddenBy = string.IsNullOrWhiteSpace(req.OverriddenBy) ? "admin" : req.OverriddenBy,
            OverrideAt = DateTimeOffset.UtcNow,
            NewDecision = decision,
            Reason = req.Reason,
            Notes = req.Notes
        };

        _db.HumanOverrides.Add(ov);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            runId = run.Id,
            originalDecision = run.FinalDecision.ToString(),
            overriddenDecision = ov.NewDecision.ToString(),
            overriddenBy = ov.OverriddenBy,
            overrideAt = ov.OverrideAt,
            reason = ov.Reason,
            notes = ov.Notes
        });
    }
}
