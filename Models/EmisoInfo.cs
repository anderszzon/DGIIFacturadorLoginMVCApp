using System.ComponentModel.DataAnnotations;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class EmisorInfo
    {
        [Key]
        public int IdEmisor { get; set; }

        // Emisor
        public string? RNCEmisor { get; set; }
        public string? RazonSocialEmisor { get; set; }
        public string? NombreComercial { get; set; }
        public string? DireccionEmisor { get; set; }
        public string? Municipio { get; set; }
        public string? Provincia { get; set; }
        public string? CorreoEmisor { get; set; }
        public string? WebSite { get; set; }
        public string? CodigoVendedor { get; set; }
        public string? NumeroFacturaInterna { get; set; }
        public string? NumeroPedidoInterno { get; set; }
        public string? ZonaVenta { get; set; }
        public string? FechaEmision { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

    }

}
