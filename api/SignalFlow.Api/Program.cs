using Microsoft.EntityFrameworkCore;
using SignalFlow.Api.Middleware;
using SignalFlow.Application.Services;
using SignalFlow.Infrastructure.Data;
using SignalFlow.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SignalFlowDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("SignalFlow")));

builder.Services.AddSingleton<SchemaValidator>();
builder.Services.AddSingleton<IModelClient, FakeModelClient>();
builder.Services.AddScoped<DecisionRunService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SignalFlowDbContext>();
    await DbSeeder.SeedAsync(db);
}

// app.UseSwagger();
// app.UseSwaggerUI();

app.UseMiddleware<TenantAuthMiddleware>();

app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapControllers();

app.Run();
