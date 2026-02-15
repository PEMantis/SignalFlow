# SignalFlow Demo Script

This demo shows:
- Create decision runs
- View AI output + policy checks + final decision
- Replay evaluation deterministically
- Apply human override

---

## 1) Start API

```bash
cd api
dotnet run --project ./SignalFlow.Api/SignalFlow.Api.csproj
```

Then in another terminal:

```bash
export SF_BASE="http://localhost:5202"
export SF_TENANT_KEY="demo-tenant-key"
```

---

## 2) Create Decision Run (Approve Case)

```bash
curl -s -X POST "$SF_BASE/api/runs" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Key: $SF_TENANT_KEY" \
  -d '{"templateKey":"credit_risk_prescreen","input":{"applicationId":"APP-APPROVE-1","requestedAmount":15000,"termMonths":36,"purpose":"debt_consolidation","applicant":{"age":34,"annualIncome":72000,"employmentLengthMonths":26,"employmentType":"w2","state":"CA"},"creditProfile":{"creditScore":640,"debtToIncomeRatio":0.41,"delinquencies12m":0,"bankruptcies":0},"declared":{"monthlyHousingPayment":2100,"monthlyDebtPayments":650}}}' | jq
```

---

## 3) Replay Evaluation

```bash
curl -s -X POST "$SF_BASE/api/runs/<RUN_ID>/replay" \
  -H "X-Tenant-Key: $SF_TENANT_KEY" | jq
```

---

## 4) Human Override

```bash
curl -s -X POST "$SF_BASE/api/runs/<RUN_ID>/override" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Key: $SF_TENANT_KEY" \
  -d '{"newDecision":"Review","reason":"Manual review","notes":"Underwriter request","overriddenBy":"perry"}' | jq
```

---

SignalFlow demonstrates structured AI output, deterministic policy enforcement, auditability, and replay.
