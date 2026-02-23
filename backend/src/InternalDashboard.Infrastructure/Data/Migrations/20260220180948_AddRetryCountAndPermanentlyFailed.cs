using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryCountAndPermanentlyFailed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "IntegrationJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "IntegrationJobs");
        }
    }
}
