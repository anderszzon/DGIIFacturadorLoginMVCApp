namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturasDGII
    {

        public int Id { get; set; }

        // Encabezado.IdDoc
        public string TipoeCF { get; set; }
        public string ENCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string TipoPago { get; set; }

        // Emisor
        public string RNCEmisor { get; set; }
        public string RazonSocialEmisor { get; set; }

        // Comprador
        public string RNCComprador { get; set; }
        public string RazonSocialComprador { get; set; }

        // Totales
        public decimal MontoGravadoTotal { get; set; }
        public decimal TotalITBIS { get; set; }
        public decimal MontoTotal { get; set; }

        // Fecha
        public string FechaHoraFirma { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

    }
}
