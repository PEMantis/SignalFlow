using Microsoft.EntityFrameworkCore;
using SignalFlow.Domain.Entities;

namespace SignalFlow.Infrastructure.Data;

public sealed class SignalFlowDbContext : DbContext
{
    public SignalFlowDbContext(DbContextOptions<SignalFlowDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantConfig> TenantConfigs => Set<TenantConfig>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<PromptTemplateVersion> PromptTemplateVersions => Set<PromptTemplateVersion>();
    public DbSet<DecisionRun> DecisionRuns => Set<DecisionRun>();
    public DbSet<PolicyEvaluation> PolicyEvaluations => Set<PolicyEvaluation>();
    public DbSet<HumanOverride> HumanOverrides => Set<HumanOverride>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>().HasKey(x => x.Id);
        b.Entity<Tenant>()
            .HasOne(x => x.Config)
            .WithOne()
            .HasForeignKey<TenantConfig>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Tenant>().Property(x => x.Name).HasMaxLength(200);
        b.Entity<Tenant>().Property(x => x.ApiKeyHash).HasMaxLength(128);

        b.Entity<PromptTemplate>().HasKey(x => x.Id);
        b.Entity<PromptTemplate>()
            .HasIndex(x => new { x.TenantId, x.Key })
            .IsUnique();

        b.Entity<PromptTemplateVersion>().HasKey(x => x.Id);
        b.Entity<PromptTemplateVersion>()
            .HasIndex(x => new { x.PromptTemplateId, x.Version })
            .IsUnique();

        b.Entity<DecisionRun>().HasKey(x => x.Id);
        b.Entity<DecisionRun>()
            .HasIndex(x => new { x.TenantId, x.CreatedAt });

        b.Entity<PolicyEvaluation>().HasKey(x => x.Id);
        b.Entity<PolicyEvaluation>()
            .HasIndex(x => x.DecisionRunId);

        b.Entity<HumanOverride>().HasKey(x => x.Id);
        b.Entity<HumanOverride>()
            .HasIndex(x => x.DecisionRunId);

        // Keep JSON as TEXT in SQLite
        b.Entity<DecisionRun>().Property(x => x.InputJson).HasColumnType("TEXT");
        b.Entity<DecisionRun>().Property(x => x.AiOutputJson).HasColumnType("TEXT");
        b.Entity<DecisionRun>().Property(x => x.SchemaErrorsJson).HasColumnType("TEXT");
        b.Entity<DecisionRun>().Property(x => x.RenderedPrompt).HasColumnType("TEXT");
    }
}
