using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finanzauto.Infrastructure.Persistence.Migrations;

public partial class AuditDatesWithoutSecurityStamp : Migration
{
    private static readonly string[] AuditedTables =
    [
        "Categories", "Customers", "Employees", "OrderDetails", "Orders",
        "Products", "Roles", "Shippers", "Suppliers"
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No borrar/recrear seeds: sus cascadas eliminarían los datos relacionados.
        migrationBuilder.DropColumn(name: "SecurityStamp", table: "Employees");
        migrationBuilder.DropColumn(name: "SecurityStamp", table: "Roles");
        foreach (var table in AuditedTables)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDate", table: table, type: "timestamp with time zone",
                nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate", table: table, type: "timestamp with time zone", nullable: true);
        }

        migrationBuilder.Sql("""
            CREATE FUNCTION public.set_audit_dates() RETURNS trigger
            LANGUAGE plpgsql AS $$
            BEGIN
                IF TG_OP = 'INSERT' THEN
                    NEW."CreationDate" := clock_timestamp();
                    NEW."UpdatedDate" := NULL;
                ELSE
                    NEW."CreationDate" := OLD."CreationDate";
                    NEW."UpdatedDate" := OLD."UpdatedDate";
                    -- Las columnas generadas aún no se calculan en un trigger BEFORE.
                    IF (to_jsonb(NEW) - ARRAY['CreationDate', 'UpdatedDate', 'SearchName', 'NormalizedName'])
                        IS DISTINCT FROM
                       (to_jsonb(OLD) - ARRAY['CreationDate', 'UpdatedDate', 'SearchName', 'NormalizedName']) THEN
                        NEW."UpdatedDate" := clock_timestamp();
                    END IF;
                END IF;
                RETURN NEW;
            END;
            $$;
            """);
        foreach (var table in AuditedTables)
        {
            migrationBuilder.Sql($"""
                CREATE TRIGGER "TR_{table}_AuditDates"
                BEFORE INSERT OR UPDATE ON public."{table}"
                FOR EACH ROW EXECUTE FUNCTION public.set_audit_dates();
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in AuditedTables)
        {
            migrationBuilder.Sql($"""DROP TRIGGER "TR_{table}_AuditDates" ON public."{table}";""");
            migrationBuilder.DropColumn(name: "CreationDate", table: table);
            migrationBuilder.DropColumn(name: "UpdatedDate", table: table);
        }
        migrationBuilder.Sql("DROP FUNCTION public.set_audit_dates();");
        migrationBuilder.AddColumn<Guid>(
            name: "SecurityStamp", table: "Employees", type: "uuid",
            nullable: false, defaultValueSql: "gen_random_uuid()");
        migrationBuilder.AddColumn<Guid>(
            name: "SecurityStamp", table: "Roles", type: "uuid",
            nullable: false, defaultValueSql: "gen_random_uuid()");
        migrationBuilder.UpdateData("Roles", "RoleId", 1, "SecurityStamp", Guid.Parse("fce6cf73-450c-4c35-af22-a1c06cda62ca"));
        migrationBuilder.UpdateData("Roles", "RoleId", 2, "SecurityStamp", Guid.Parse("a21f876a-b676-44ef-9f73-70e29b7cb7e4"));
    }
}
