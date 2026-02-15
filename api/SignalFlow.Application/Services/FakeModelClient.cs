using System.Diagnostics;
using System.Text.Json;

namespace SignalFlow.Application.Services;

public sealed class FakeModelClient : IModelClient
{
    public Task<ModelResponse> GenerateAsync(ModelRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var doc = JsonDocument.Parse(request.InputJson);

        var credit = doc.RootElement.GetProperty("creditProfile");
        var score = credit.GetProperty("creditScore").GetInt32();
        var dti = credit.GetProperty("debtToIncomeRatio").GetDecimal();
        var bankruptcies = credit.GetProperty("bankruptcies").GetInt32();

        var (risk, decision, conf) = bankruptcies > 0 ? ("High","Deny",0.92m)
            : score >= 700 && dti < 0.35m ? ("Low","Approve",0.80m)
            : score >= 620 && dti <= 0.43m ? ("Medium","Approve",0.66m)
            : ("Medium","Review",0.58m);

        var output = new
        {
            riskLevel = risk,
            confidence = (double)conf,
            reasons = new object[]
            {
                new { code = "CREDIT_SCORE", summary = "Credit score considered in prescreen.", evidence = new { creditScore = score } },
                new { code = "DTI", summary = "Debt-to-income ratio considered in prescreen.", evidence = new { debtToIncomeRatio = dti } }
            },
            recommendedDecision = decision,
            recommendedMaxAmount = 25000,
            recommendedAPR = 0.18,
            policyFlags = conf < 0.60m ? new[] { "LOW_CONFIDENCE" } : Array.Empty<string>()
        };

        var outputJson = JsonSerializer.Serialize(output);

        sw.Stop();

        var usage = new ModelUsage(PromptTokens: 500, CompletionTokens: 180, TotalTokens: 680);
        return Task.FromResult(new ModelResponse(outputJson, (int)sw.ElapsedMilliseconds, usage, request.Model));
    }
}
