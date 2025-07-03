using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    //public class FacturaDGIIResponseModel3
    //{
    //    public string JsonInvoice { get; set; }
    //    public string ENCF { get; set; }
    //    public string XmlSemilla { get; set; }
    //    public string XmlSemillaFirmada { get; set; }
    //    public string Token { get; set; }
    //    public string XmlFactura { get; set; }
    //    public string XmlFacturaFirmada { get; set; }
    //    public string CodigoSeguridad { get; set; }
    //    public string CodigoRespuesta { get; set; }
    //    public string EstadoRespuesta { get; set; }
    //    public string Mensaje { get; set; }

    //}

    public class FacturaDGIIModel3
    {
        public ECFModel3 ECF { get; set; } = new ECFModel3();
    }

    public class ECFModel3
    {
        public EncabezadoModel3 Encabezado { get; set; } = new EncabezadoModel3();
        public DetallesItemsModel3 DetallesItems { get; set; } = new DetallesItemsModel3();
        public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModel3
    {
        public string Version { get; set; }

        public VersionIdDocModel3 IdDoc { get; set; } = new VersionIdDocModel3();
        public EmisorModel3 Emisor { get; set; } = new EmisorModel3();
        public CompradorModel3 Comprador { get; set; } = new CompradorModel3();
        public TotalesModel3 Totales { get; set; } = new TotalesModel3();
    }

    public class VersionIdDocModel3
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModel3
    {
        public string RNCEmisor { get; set; }
        public string RazonSocialEmisor { get; set; }
        public string NombreComercial { get; set; }
        public string DireccionEmisor { get; set; }
        public string Municipio { get; set; }
        public string Provincia { get; set; }
        public string CorreoEmisor { get; set; }
        public string WebSite { get; set; }
        public string CodigoVendedor { get; set; }
        public string NumeroFacturaInterna { get; set; }
        public string NumeroPedidoInterno { get; set; }
        public string ZonaVenta { get; set; }
        public string FechaEmision { get; set; }
    }

    public class CompradorModel3
    {
        public string RNCComprador { get; set; }
        public string RazonSocialComprador { get; set; }
        public string ContactoComprador { get; set; }
        public string CorreoComprador { get; set; }
        public string DireccionComprador { get; set; }
        public string MunicipioComprador { get; set; }
        public string ProvinciaComprador { get; set; }
        public string FechaEntrega { get; set; }
        public string FechaOrdenCompra { get; set; }
        public string NumeroOrdenCompra { get; set; }
        public string CodigoInternoComprador { get; set; }
    }

    public class TotalesModel3
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string ITBIS1 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string MontoImpuestoAdicional { get; set; }
        public ImpuestosAdicionalesModel3 ImpuestosAdicionales { get; set; }
        public string MontoTotal { get; set; }

    }

    public class ImpuestosAdicionalesModel3
    {
        public List<ImpuestoAdicionalTotalesModel3> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel3
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel3
    {
        public List<ItemModel3> Item { get; set; } = new List<ItemModel3>();
    }

    public class ItemModel3
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public TablaImpuestoAdicionalModel3 TablaImpuestoAdicional { get; set; }
        public string MontoItem { get; set; }

    }

    public class TablaImpuestoAdicionalModel3
    {
        public List<ImpuestoAdicionalItemModel3> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModel3
    {
        public string TipoImpuesto { get; set; }
    }

}
