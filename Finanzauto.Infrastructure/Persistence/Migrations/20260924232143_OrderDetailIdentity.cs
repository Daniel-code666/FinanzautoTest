using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Finanzauto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderDetailIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderDetails",
                table: "OrderDetails");

            migrationBuilder.AddColumn<int>(
                name: "OrderDetailId",
                table: "OrderDetails",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderDetails",
                table: "OrderDetails",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderId_ProductId",
                table: "OrderDetails",
                columns: new[] { "OrderId", "ProductId" },
                unique: true,
                filter: "\"Active\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderDetails",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_OrderId_ProductId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "OrderDetailId",
                table: "OrderDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderDetails",
                table: "OrderDetails",
                columns: new[] { "OrderId", "ProductId" });
        }
    }
}
