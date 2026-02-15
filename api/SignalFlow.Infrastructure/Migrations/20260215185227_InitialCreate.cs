using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecisionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    InputJson = table.Column<string>(type: "TEXT", nullable: false),
                    RenderedPrompt = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    LatencyMs = table.Column<int>(type: "INTEGER", nullable: false),
                    PromptTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletionTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    AiOutputJson = table.Column<string>(type: "TEXT", nullable: false),
                    SchemaValid = table.Column<bool>(type: "INTEGER", nullable: false),
                    SchemaErrorsJson = table.Column<string>(type: "TEXT", nullable: true),
                    FinalDecision = table.Column<int>(type: "INTEGER", nullable: false),
                    DecisionReason = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyPassed = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuditHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HumanOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DecisionRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OverriddenBy = table.Column<string>(type: "TEXT", nullable: false),
                    OverrideAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    NewDecision = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumanOverrides_DecisionRuns_DecisionRunId",
                        column: x => x.DecisionRunId,
                        principalTable: "DecisionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DecisionRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PolicyKey = table.Column<string>(type: "TEXT", nullable: false),
                    Passed = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetailsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyEvaluations_DecisionRuns_DecisionRunId",
                        column: x => x.DecisionRunId,
                        principalTable: "DecisionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PromptTemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    OutputSchemaJson = table.Column<string>(type: "TEXT", nullable: false),
                    InputSchemaJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptTemplateVersions_PromptTemplates_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalTable: "PromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultModel = table.Column<string>(type: "TEXT", nullable: false),
                    RequireSchemaValid = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowHumanOverride = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinConfidence = table.Column<decimal>(type: "TEXT", nullable: false),
                    HardMinCreditScore = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewCreditScoreBelow = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewDTIAbove = table.Column<decimal>(type: "TEXT", nullable: false),
                    HardMaxDTI = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxDelinquencies12m = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRequestedAmount = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantConfigs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionRuns_TenantId_CreatedAt",
                table: "DecisionRuns",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HumanOverrides_DecisionRunId",
                table: "HumanOverrides",
                column: "DecisionRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvaluations_DecisionRunId",
                table: "PolicyEvaluations",
                column: "DecisionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_TenantId_Key",
                table: "PromptTemplates",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplateVersions_PromptTemplateId_Version",
                table: "PromptTemplateVersions",
                columns: new[] { "PromptTemplateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigs_TenantId",
                table: "TenantConfigs",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HumanOverrides");

            migrationBuilder.DropTable(
                name: "PolicyEvaluations");

            migrationBuilder.DropTable(
                name: "PromptTemplateVersions");

            migrationBuilder.DropTable(
                name: "TenantConfigs");

            migrationBuilder.DropTable(
                name: "DecisionRuns");

            migrationBuilder.DropTable(
                name: "PromptTemplates");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
