using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NJobDesk.History.EFCore.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NJobDeskExecutionHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FireInstanceId = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    SchedulerInstanceId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    JobName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    JobGroup = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    TriggerId = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    TriggerName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Recovering = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NJobDeskExecutionHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NJobDeskExecutionLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExecutionId = table.Column<long>(type: "INTEGER", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NJobDeskExecutionLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NJobDeskExecutionLog_NJobDeskExecutionHistory_ExecutionId",
                        column: x => x.ExecutionId,
                        principalTable: "NJobDeskExecutionHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionHistory_ProviderKey_FireInstanceId",
                table: "NJobDeskExecutionHistory",
                columns: new[] { "ProviderKey", "FireInstanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionHistory_ProviderKey_JobId_StartedUtc",
                table: "NJobDeskExecutionHistory",
                columns: new[] { "ProviderKey", "JobId", "StartedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionHistory_StartedUtc",
                table: "NJobDeskExecutionHistory",
                column: "StartedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionHistory_Status",
                table: "NJobDeskExecutionHistory",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionLog_ExecutionId",
                table: "NJobDeskExecutionLog",
                column: "ExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NJobDeskExecutionLog");

            migrationBuilder.DropTable(
                name: "NJobDeskExecutionHistory");
        }
    }
}
