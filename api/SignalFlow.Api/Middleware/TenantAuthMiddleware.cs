using Microsoft.EntityFrameworkCore;
using SignalFlow.Infrastructure.Data;
using SignalFlow.Infrastructure.Auth;

namespace SignalFlow.Api.Middleware;

public sealed class TenantAuthMiddleware
{
    private readonly RequestDelegate _next;

    public TenantAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, SignalFlowDbContext db)
    {
        // Allow swagger + health without auth in MVP
        var path = ctx.Request.Path.Value ?? "";
        if (path.StartsWith("/swagger") || path.StartsWith("/health"))
        {
            await _next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue("X-Tenant-Key", out var key) || string.IsNullOrWhiteSpace(key))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Missing X-Tenant-Key");
            return;
        }

        var hash = ApiKeyHasher.Sha256Hex(key!);
        var tenant = await db.Tenants.Include(t => t.Config).FirstOrDefaultAsync(t => t.ApiKeyHash == hash);
        if (tenant is null)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Invalid tenant key");
            return;
        }

        ctx.Items["TenantId"] = tenant.Id;
        ctx.Items["TenantConfig"] = tenant.Config;

        await _next(ctx);
    }
}
