using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DGIIFacturadorLoginMVCApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class detailsitems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemsFactura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacturaId = table.Column<int>(type: "int", nullable: false),
                    NumeroLinea = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicadorFacturacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreItem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicadorBienoServicio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CantidadItem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecioUnitarioItem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoItem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FacturasDGIIId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsFactura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemsFactura_FacturasDGII_FacturasDGIIId",
                        column: x => x.FacturasDGIIId,
                        principalTable: "FacturasDGII",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsFactura_FacturasDGIIId",
                table: "ItemsFactura",
                column: "FacturasDGIIId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemsFactura");
        }
    }
}
