using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DGIIFacturadorLoginMVCApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class incluirEmisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemsFactura_FacturasDGII_FacturasDGIIId",
                table: "ItemsFactura");

            migrationBuilder.DropIndex(
                name: "IX_ItemsFactura_FacturasDGIIId",
                table: "ItemsFactura");

            migrationBuilder.DropColumn(
                name: "FacturasDGIIId",
                table: "ItemsFactura");

            migrationBuilder.CreateTable(
                name: "EmisorInfo",
                columns: table => new
                {
                    IdEmisor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RNCEmisor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RazonSocialEmisor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreComercial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DireccionEmisor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Municipio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provincia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorreoEmisor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebSite = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoVendedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroFacturaInterna = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumeroPedidoInterno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZonaVenta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaEmision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmisorInfo", x => x.IdEmisor);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsFactura_FacturaId",
                table: "ItemsFactura",
                column: "FacturaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemsFactura_FacturasDGII_FacturaId",
                table: "ItemsFactura",
                column: "FacturaId",
                principalTable: "FacturasDGII",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemsFactura_FacturasDGII_FacturaId",
                table: "ItemsFactura");

            migrationBuilder.DropTable(
                name: "EmisorInfo");

            migrationBuilder.DropIndex(
                name: "IX_ItemsFactura_FacturaId",
                table: "ItemsFactura");

            migrationBuilder.AddColumn<int>(
                name: "FacturasDGIIId",
                table: "ItemsFactura",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemsFactura_FacturasDGIIId",
                table: "ItemsFactura",
                column: "FacturasDGIIId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemsFactura_FacturasDGII_FacturasDGIIId",
                table: "ItemsFactura",
                column: "FacturasDGIIId",
                principalTable: "FacturasDGII",
                principalColumn: "Id");
        }
    }
}
