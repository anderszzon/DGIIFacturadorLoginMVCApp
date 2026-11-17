using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DGIIFacturadorLoginMVCApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class codigoitemfactura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoItem",
                table: "ItemsFactura",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoItem",
                table: "ItemsFactura");
        }
    }
}
