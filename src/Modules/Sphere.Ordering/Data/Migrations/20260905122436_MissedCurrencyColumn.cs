using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sphere.Ordering.Data.Migrations
{
    /// <inheritdoc />
    public partial class MissedCurrencyColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Currency",
                schema: "ordering",
                table: "orders",
                newName: "currency");

            migrationBuilder.AlterColumn<string>(
                name: "currency",
                schema: "ordering",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "currency",
                schema: "ordering",
                table: "orders",
                newName: "Currency");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "ordering",
                table: "orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);
        }
    }
}
