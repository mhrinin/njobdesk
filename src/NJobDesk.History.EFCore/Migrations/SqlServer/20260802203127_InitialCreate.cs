using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NJobDesk.History.EFCore.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "njobdesk");

            migrationBuilder.CreateTable(
                name: "NJobDeskExecutionHistory",
                schema: "njobdesk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FireInstanceId = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    SchedulerInstanceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JobGroup = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TriggerId = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TriggerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Recovering = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NJobDeskExecutionHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NJobDeskExecutionLog",
                schema: "njobdesk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExecutionId = table.Column<long>(type: "bigint", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NJobDeskExecutionLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NJobDeskExecutionLog_NJobDeskExecutionHistory_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "njobdesk",
                        principalTable: "NJobDeskExecutionHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionHistory_ProviderKey_FireInstanceId",
                schema: "njobdesk",
                table: "NJobDeskExecutionHistory",
                columns: new[] { "ProviderKey", "FireInstanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionHistory_ProviderKey_JobId_StartedUtc",
                schema: "njobdesk",
                table: "NJobDeskExecutionHistory",
                columns: new[] { "ProviderKey", "JobId", "StartedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionHistory_StartedUtc",
                schema: "njobdesk",
                table: "NJobDeskExecutionHistory",
                column: "StartedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionHistory_Status",
                schema: "njobdesk",
                table: "NJobDeskExecutionHistory",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NJobDeskExecutionLog_ExecutionId",
                schema: "njobdesk",
                table: "NJobDeskExecutionLog",
                column: "ExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NJobDeskExecutionLog",
                schema: "njobdesk");

            migrationBuilder.DropTable(
                name: "NJobDeskExecutionHistory",
                schema: "njobdesk");
        }
    }
}
