using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sphere.Catalog.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatalogBrowseIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_products_category_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id_name",
                schema: "catalog",
                table: "products",
                columns: new[] { "category_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_products_name_id",
                schema: "catalog",
                table: "products",
                columns: new[] { "name", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_products_category_id_name",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_name_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                schema: "catalog",
                table: "products",
                column: "category_id");
        }
    }
}
