namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIResponseModel
    {
        public string JsonInvoice { get; set; }
        public string ENCF { get; set; }
        public string XmlSemilla { get; set; }
        public string XmlSemillaFirmada { get; set; }
        public string Token { get; set; }
        public string XmlFactura { get; set; }
        public string XmlFacturaFirmada { get; set; }
        public string CodigoSeguridad { get; set; }
        public string CodigoRespuesta { get; set; }
        public string EstadoRespuesta { get; set; }
        public string Mensaje { get; set; }

    }
}
