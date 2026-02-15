using System.Text.Json;
using SignalFlow.Domain.Entities;
using SignalFlow.Domain.Enums;

namespace SignalFlow.Application.Policies;

public sealed record PolicyResult(string PolicyKey, bool Passed, string DetailsJson, DecisionType? SuggestedOutcome);

public static class CreditRiskPolicies
{
    public static List<PolicyResult> Evaluate(TenantConfig cfg, string inputJson, string aiOutputJson, bool schemaValid)
    {
        var results = new List<PolicyResult>();

        // Schema gate (growth: schema invalid -> Review, not Deny)
        if (cfg.RequireSchemaValid)
        {
            results.Add(new PolicyResult(
                "SchemaValid",
                schemaValid,
                $"{{\"requireSchemaValid\":true,\"schemaValid\":{schemaValid.ToString().ToLowerInvariant()}}}",
                schemaValid ? null : DecisionType.Review
            ));
        }

        using var inputDoc = JsonDocument.Parse(inputJson);
        var credit = inputDoc.RootElement.GetProperty("creditProfile");
        var score = credit.GetProperty("creditScore").GetInt32();
        var dti = credit.GetProperty("debtToIncomeRatio").GetDecimal();
        var delinq = credit.GetProperty("delinquencies12m").GetInt32();
        var bankruptcies = credit.GetProperty("bankruptcies").GetInt32();

        // Hard deny: bankruptcy
        if (bankruptcies > 0)
        {
            results.Add(new PolicyResult("HardDeny_Bankruptcy", false,
                $"{{\"bankruptcies\":{bankruptcies}}}", DecisionType.Deny));
        }
        else results.Add(new PolicyResult("HardDeny_Bankruptcy", true, $"{{\"bankruptcies\":0}}", null));

        // Hard deny: credit score
        if (score < cfg.HardMinCreditScore)
        {
            results.Add(new PolicyResult("HardDeny_CreditScore", false,
                $"{{\"creditScore\":{score},\"hardMin\":{cfg.HardMinCreditScore}}}", DecisionType.Deny));
        }
        else results.Add(new PolicyResult("HardDeny_CreditScore", true,
            $"{{\"creditScore\":{score},\"hardMin\":{cfg.HardMinCreditScore}}}", null));

        // Review: borderline score
        if (score < cfg.ReviewCreditScoreBelow)
        {
            results.Add(new PolicyResult("Review_BorderlineCreditScore", false,
                $"{{\"creditScore\":{score},\"reviewBelow\":{cfg.ReviewCreditScoreBelow}}}", DecisionType.Review));
        }
        else results.Add(new PolicyResult("Review_BorderlineCreditScore", true,
            $"{{\"creditScore\":{score},\"reviewBelow\":{cfg.ReviewCreditScoreBelow}}}", null));

        // DTI hard/review
        if (dti > cfg.HardMaxDTI)
        {
            results.Add(new PolicyResult("HardDeny_DTI", false,
                $"{{\"dti\":{dti},\"hardMax\":{cfg.HardMaxDTI}}}", DecisionType.Deny));
        }
        else results.Add(new PolicyResult("HardDeny_DTI", true,
            $"{{\"dti\":{dti},\"hardMax\":{cfg.HardMaxDTI}}}", null));

        if (dti > cfg.ReviewDTIAbove)
        {
            results.Add(new PolicyResult("Review_DTI", false,
                $"{{\"dti\":{dti},\"reviewAbove\":{cfg.ReviewDTIAbove}}}", DecisionType.Review));
        }
        else results.Add(new PolicyResult("Review_DTI", true,
            $"{{\"dti\":{dti},\"reviewAbove\":{cfg.ReviewDTIAbove}}}", null));

        // Delinquencies review
        if (delinq > cfg.MaxDelinquencies12m)
        {
            results.Add(new PolicyResult("Review_Delinquencies12m", false,
                $"{{\"delinquencies12m\":{delinq},\"max\":{cfg.MaxDelinquencies12m}}}", DecisionType.Review));
        }
        else results.Add(new PolicyResult("Review_Delinquencies12m", true,
            $"{{\"delinquencies12m\":{delinq},\"max\":{cfg.MaxDelinquencies12m}}}", null));

        // Confidence check (only if schema valid, otherwise AI JSON may not parse)
        if (schemaValid)
        {
            using var aiDoc = JsonDocument.Parse(aiOutputJson);
            var conf = aiDoc.RootElement.GetProperty("confidence").GetDecimal();

            if (conf < cfg.MinConfidence)
            {
                results.Add(new PolicyResult("Review_LowConfidence", false,
                    $"{{\"confidence\":{conf},\"min\":{cfg.MinConfidence}}}", DecisionType.Review));
            }
            else results.Add(new PolicyResult("Review_LowConfidence", true,
                $"{{\"confidence\":{conf},\"min\":{cfg.MinConfidence}}}", null));
        }

        return results;
    }

    public static (DecisionType Decision, string Reason, bool Passed) Resolve(List<PolicyResult> policies)
    {
        var denies = policies.Where(p => p.SuggestedOutcome == DecisionType.Deny).ToList();
        if (denies.Count > 0) return (DecisionType.Deny, denies[0].PolicyKey, false);

        var reviews = policies.Where(p => p.SuggestedOutcome == DecisionType.Review).ToList();
        if (reviews.Count > 0) return (DecisionType.Review, reviews[0].PolicyKey, false);

        return (DecisionType.Approve, "AllPoliciesPassed", true);
    }
}
