using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRulesPriorityTypeToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create temporary integer column
            migrationBuilder.AddColumn<int>(
                name: "Priority_new",
                table: "Rules",
                type: "integer",
                nullable: false,
                defaultValue: 20);  // Default to Medium

            // Convert existing string values to integers
            migrationBuilder.Sql(
                @"UPDATE ""Rules"" SET ""Priority_new"" = CASE 
                    WHEN ""Priority"" = 'Low' THEN 10
                    WHEN ""Priority"" = 'Medium' THEN 20
                    WHEN ""Priority"" = 'High' THEN 30
                    ELSE 20
                END;");

            // Drop old column and rename new one
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Rules");

            migrationBuilder.RenameColumn(
                name: "Priority_new",
                table: "Rules",
                newName: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Create temporary string column
            migrationBuilder.AddColumn<string>(
                name: "Priority_old",
                table: "Rules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Medium");

            // Convert back to strings
            migrationBuilder.Sql(
                @"UPDATE ""Rules"" SET ""Priority_old"" = CASE 
                    WHEN ""Priority"" = 10 THEN 'Low'
                    WHEN ""Priority"" = 20 THEN 'Medium'
                    WHEN ""Priority"" = 30 THEN 'High'
                    ELSE 'Medium'
                END;");

            // Drop integer column and rename old one
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Rules");

            migrationBuilder.RenameColumn(
                name: "Priority_old",
                table: "Rules",
                newName: "Priority");
        }
    }
}
