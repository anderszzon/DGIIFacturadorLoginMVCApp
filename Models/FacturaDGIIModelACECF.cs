using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModelACECF
    {
        public ECFModelACECF ACECF { get; set; } = new ECFModelACECF();
    }

    public class ECFModelACECF
    {
        public DetalleAprobacionComercialACECF DetalleAprobacionComercial { get; set; } = new DetalleAprobacionComercialACECF();
    }

    public class DetalleAprobacionComercialACECF
    {
        public string Version { get; set; }
        public string RNCEmisor { get; set; }
        public string eNCF { get; set; }
        public string FechaEmision { get; set; }
        public string MontoTotal { get; set; }
        public string RNCComprador { get; set; }
        public string Estado { get; set; }
        public string FechaHoraAprobacionComercial { get; set; }
    }

}
