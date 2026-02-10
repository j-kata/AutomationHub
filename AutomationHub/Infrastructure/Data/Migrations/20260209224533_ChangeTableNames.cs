using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RuleAction_Rules_RuleId",
                table: "RuleAction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RuleAction",
                table: "RuleAction");

            migrationBuilder.RenameTable(
                name: "RuleAction",
                newName: "RuleActions");

            migrationBuilder.RenameIndex(
                name: "IX_RuleAction_RuleId",
                table: "RuleActions",
                newName: "IX_RuleActions_RuleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RuleActions",
                table: "RuleActions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RuleActions_Rules_RuleId",
                table: "RuleActions",
                column: "RuleId",
                principalTable: "Rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RuleActions_Rules_RuleId",
                table: "RuleActions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RuleActions",
                table: "RuleActions");

            migrationBuilder.RenameTable(
                name: "RuleActions",
                newName: "RuleAction");

            migrationBuilder.RenameIndex(
                name: "IX_RuleActions_RuleId",
                table: "RuleAction",
                newName: "IX_RuleAction_RuleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RuleAction",
                table: "RuleAction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RuleAction_Rules_RuleId",
                table: "RuleAction",
                column: "RuleId",
                principalTable: "Rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
