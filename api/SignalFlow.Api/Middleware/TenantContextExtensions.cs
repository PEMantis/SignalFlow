using SignalFlow.Domain.Entities;

namespace SignalFlow.Api.Middleware;

public static class TenantContextExtensions
{
    public static Guid TenantId(this HttpContext ctx) => (Guid)ctx.Items["TenantId"]!;
    public static TenantConfig TenantConfig(this HttpContext ctx) => (TenantConfig)ctx.Items["TenantConfig"]!;
}
