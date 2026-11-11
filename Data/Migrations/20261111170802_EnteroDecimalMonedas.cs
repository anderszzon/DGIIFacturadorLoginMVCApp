using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DGIIFacturadorLoginMVCApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnteroDecimalMonedas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TipoCambio",
                table: "Monedas",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TipoCambio",
                table: "Monedas",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }
    }
}
