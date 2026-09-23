using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finanzauto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentitySecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                table: "Roles",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                table: "Employees",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Roles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                computedColumnSql: "upper(btrim(\"Name\"))",
                stored: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "SecurityStamp",
                value: new Guid("fce6cf73-450c-4c35-af22-a1c06cda62ca"));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "SecurityStamp",
                value: new Guid("a21f876a-b676-44ef-9f73-70e29b7cb7e4"));

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Employees");
        }
    }
}
