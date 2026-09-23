using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finanzauto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogSearchAndSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "SearchName",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                computedColumnSql: "upper(\"ProductName\")",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                computedColumnSql: "upper(btrim(\"CategoryName\"))",
                stored: true);

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "SupplierId", "Active", "Address", "City", "CompanyName", "ContactName", "ContactTitle", "Country", "Fax", "HomePage", "Phone", "PostalCode", "Region" },
                values: new object[] { 1, true, null, null, "Proveedor inicial", null, null, null, null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Active_UnitPrice_ProductId",
                table: "Products",
                columns: new[] { "Active", "UnitPrice", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchName",
                table: "Products",
                column: "SearchName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_NormalizedName",
                table: "Categories",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Active_UnitPrice_ProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SearchName",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_NormalizedName",
                table: "Categories");

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "SupplierId",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "SearchName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Categories");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
