using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DGIIFacturadorLoginMVCApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class invoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacturasDGII",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoeCF = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ENCF = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaVencimientoSecuencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoPago = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RNCEmisor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RazonSocialEmisor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RNCComprador = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RazonSocialComprador = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MontoGravadoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalITBIS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaHoraFirma = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasDGII", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturasDGII");
        }
    }
}
