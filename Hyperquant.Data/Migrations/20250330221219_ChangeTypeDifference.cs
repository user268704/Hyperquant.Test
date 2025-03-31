using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hyperquant.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTypeDifference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Difference",
                table: "FuturesUpdates",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Difference",
                table: "FuturesUpdates",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
