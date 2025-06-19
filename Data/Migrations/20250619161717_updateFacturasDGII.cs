using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DGIIFacturadorLoginMVCApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateFacturasDGII : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoInternoComprador",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CodigoVendedor",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactoComprador",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorreoComprador",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorreoEmisor",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DireccionComprador",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DireccionEmisor",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FechaEmision",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FechaEntrega",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FechaOrdenCompra",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ITBIS1",
                table: "FacturasDGII",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IndicadorEnvioDiferido",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IndicadorMontoGravado",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoGravadoI1",
                table: "FacturasDGII",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MunicipioComprador",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NombreComercial",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroFacturaInterna",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroOrdenCompra",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroPedidoInterno",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provincia",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProvinciaComprador",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoIngresos",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalITBIS1",
                table: "FacturasDGII",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WebSite",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZonaVenta",
                table: "FacturasDGII",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoInternoComprador",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "CodigoVendedor",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "ContactoComprador",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "CorreoComprador",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "CorreoEmisor",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "DireccionComprador",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "DireccionEmisor",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "FechaEmision",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "FechaEntrega",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "FechaOrdenCompra",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "ITBIS1",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "IndicadorEnvioDiferido",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "IndicadorMontoGravado",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "MontoGravadoI1",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "MunicipioComprador",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "NombreComercial",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "NumeroFacturaInterna",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "NumeroOrdenCompra",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "NumeroPedidoInterno",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "Provincia",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "ProvinciaComprador",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "TipoIngresos",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "TotalITBIS1",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "WebSite",
                table: "FacturasDGII");

            migrationBuilder.DropColumn(
                name: "ZonaVenta",
                table: "FacturasDGII");
        }
    }
}
