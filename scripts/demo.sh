#!/usr/bin/env bash
set -euo pipefail

SF_BASE="${SF_BASE:-http://localhost:5202}"
SF_TENANT_KEY="${SF_TENANT_KEY:-demo-tenant-key}"

echo "Using SF_BASE=$SF_BASE"
echo "Using X-Tenant-Key=$SF_TENANT_KEY"
echo

approve_payload='{"templateKey":"credit_risk_prescreen","input":{"applicationId":"APP-APPROVE-1","requestedAmount":15000,"termMonths":36,"purpose":"debt_consolidation","applicant":{"age":34,"annualIncome":72000,"employmentLengthMonths":26,"employmentType":"w2","state":"CA"},"creditProfile":{"creditScore":640,"debtToIncomeRatio":0.41,"delinquencies12m":0,"bankruptcies":0},"declared":{"monthlyHousingPayment":2100,"monthlyDebtPayments":650}}}'
deny_payload='{"templateKey":"credit_risk_prescreen","input":{"applicationId":"APP-DENY-1","requestedAmount":12000,"termMonths":24,"purpose":"other","applicant":{"age":29,"annualIncome":68000,"employmentLengthMonths":18,"employmentType":"w2","state":"CA"},"creditProfile":{"creditScore":540,"debtToIncomeRatio":0.32,"delinquencies12m":0,"bankruptcies":0},"declared":{"monthlyHousingPayment":1600,"monthlyDebtPayments":300}}}'
review_payload='{"templateKey":"credit_risk_prescreen","input":{"applicationId":"APP-REVIEW-1","requestedAmount":18000,"termMonths":48,"purpose":"debt_consolidation","applicant":{"age":41,"annualIncome":90000,"employmentLengthMonths":14,"employmentType":"w2","state":"CA"},"creditProfile":{"creditScore":625,"debtToIncomeRatio":0.49,"delinquencies12m":1,"bankruptcies":0},"declared":{"monthlyHousingPayment":2400,"monthlyDebtPayments":1400}}}'

post_run () {
  curl -s -X POST "$SF_BASE/api/runs" \
    -H "Content-Type: application/json" \
    -H "X-Tenant-Key: $SF_TENANT_KEY" \
    -d "$1"
}

echo "1) Approve run"
approve_json="$(post_run "$approve_payload")"
approve_id="$(echo "$approve_json" | sed -n 's/.*"runId":[[:space:]]*"\([^"]*\)".*/\1/p')"
echo "$approve_json" | head -c 220; echo
echo "runId=$approve_id"
echo

echo "2) Deny run"
deny_json="$(post_run "$deny_payload")"
deny_id="$(echo "$deny_json" | sed -n 's/.*"runId":[[:space:]]*"\([^"]*\)".*/\1/p')"
echo "$deny_json" | head -c 220; echo
echo "runId=$deny_id"
echo

echo "3) Review run"
review_json="$(post_run "$review_payload")"
review_id="$(echo "$review_json" | sed -n 's/.*"runId":[[:space:]]*"\([^"]*\)".*/\1/p')"
echo "$review_json" | head -c 220; echo
echo "runId=$review_id"
echo

echo "4) Replay approve run"
curl -s -X POST "$SF_BASE/api/runs/$approve_id/replay" \
  -H "X-Tenant-Key: $SF_TENANT_KEY" | head -c 220; echo
echo

echo "5) Override approve run -> Review"
curl -s -X POST "$SF_BASE/api/runs/$approve_id/override" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Key: $SF_TENANT_KEY" \
  -d '{"newDecision":"Review","reason":"Manual review requested","notes":"Demo override","overriddenBy":"perry"}' | head -c 220; echo
echo

echo "Done."
