using Microsoft.EntityFrameworkCore;
using SignalFlow.Domain.Entities;
using SignalFlow.Domain.Enums;
using SignalFlow.Infrastructure.Auth;
using SignalFlow.Infrastructure.Data;

namespace SignalFlow.Infrastructure.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(SignalFlowDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Tenants.AnyAsync()) return;

        var tenantId = Guid.NewGuid();
        var apiKey = "demo-tenant-key"; // use this in header X-Tenant-Key

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Demo Lender (Growth)",
            ApiKeyHash = ApiKeyHasher.Sha256Hex(apiKey),
            CreatedAt = DateTimeOffset.UtcNow,
            Config = new TenantConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                DefaultModel = "fake-model-v1",
                RequireSchemaValid = true,
                AllowHumanOverride = true,
                MinConfidence = 0.60m,
                HardMinCreditScore = 580,
                ReviewCreditScoreBelow = 620,
                ReviewDTIAbove = 0.43m,
                HardMaxDTI = 0.55m,
                MaxDelinquencies12m = 2,
                MaxRequestedAmount = 25000,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        var template = new PromptTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Key = "credit_risk_prescreen",
            Name = "Credit Risk Prescreen",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var schemaJson = /* JSON schema string */ """
        {
          "$schema": "http://json-schema.org/draft-07/schema#",
          "type": "object",
          "required": ["riskLevel", "confidence", "recommendedDecision", "reasons"],
          "properties": {
            "riskLevel": { "type": "string", "enum": ["Low", "Medium", "High"] },
            "confidence": { "type": "number", "minimum": 0, "maximum": 1 },
            "recommendedDecision": { "type": "string", "enum": ["Approve", "Deny", "Review"] },
            "recommendedMaxAmount": { "type": "number", "minimum": 0 },
            "recommendedAPR": { "type": "number", "minimum": 0, "maximum": 1 },
            "policyFlags": { "type": "array", "items": { "type": "string" } },
            "reasons": {
              "type": "array",
              "minItems": 1,
              "items": {
                "type": "object",
                "required": ["code", "summary"],
                "properties": {
                  "code": {
                    "type": "string",
                    "enum": ["CREDIT_SCORE","DTI","DELINQUENCY","BANKRUPTCY","INCOME","AMOUNT","MISSING_FIELD","OTHER"]
                  },
                  "summary": { "type": "string", "minLength": 3 },
                  "evidence": { "type": "object" }
                },
                "additionalProperties": false
              }
            }
          },
          "additionalProperties": false
        }
        """;

        var promptContent = """
        SYSTEM:
        You are a credit risk prescreen assistant. You MUST output ONLY valid JSON.
        No markdown. No prose. No code fences. JSON only.

        USER:
        Evaluate this loan application snapshot and return JSON that matches this schema:
        {{OutputSchemaJson}}

        Rules:
        - If uncertain, choose recommendedDecision="Review" and lower confidence.
        - Provide 1-5 reasons with codes.
        - Keep evidence minimal and numeric when possible.
        - Do not invent fields not present in the input.

        Input application JSON:
        {{InputJson}}
        """;

        var version = new PromptTemplateVersion
        {
            Id = Guid.NewGuid(),
            PromptTemplateId = template.Id,
            Version = 1,
            Status = TemplateStatus.Published,
            Content = promptContent,
            OutputSchemaJson = schemaJson,
            CreatedBy = "seed",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await db.Tenants.AddAsync(tenant);
        await db.PromptTemplates.AddAsync(template);
        await db.PromptTemplateVersions.AddAsync(version);

        await db.SaveChangesAsync();
    }
}
