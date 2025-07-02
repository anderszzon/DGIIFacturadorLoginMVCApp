using System.Text.Json.Serialization;

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

    public class FacturaDGIIModel2
    {
        public ECFModel ECF { get; set; } = new ECFModel();
    }

    public class ECFModel
    {
        public EncabezadoModel Encabezado { get; set; } = new EncabezadoModel();
        public DetallesItemsModel DetallesItems { get; set; } = new DetallesItemsModel();
        public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModel
    {
        public string Version { get; set; }

        public VersionIdDocModel IdDoc { get; set; } = new VersionIdDocModel();
        public EmisorModel Emisor { get; set; } = new EmisorModel();
        public CompradorModel Comprador { get; set; } = new CompradorModel();
        public TotalesModel Totales { get; set; } = new TotalesModel();
    }

    public class VersionIdDocModel
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModel
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

    public class CompradorModel
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

    public class TotalesModel
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string ITBIS1 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string MontoImpuestoAdicional { get; set; }
        public ImpuestosAdicionalesModel ImpuestosAdicionales { get; set; }
        public string MontoTotal { get; set; }

    }

    public class ImpuestosAdicionalesModel
    {
        public List<ImpuestoAdicionalTotalesModel> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MontoImpuestoSelectivoConsumoEspecifico { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
    }

    public class DetallesItemsModel
    {
        public List<ItemModel> Item { get; set; } = new List<ItemModel>();
    }

    public class ItemModel
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string CantidadReferencia { get; set; }
        public string UnidadReferencia { get; set; }
        public TablaSubcantidadModel TablaSubcantidad { get; set; }
        public string GradosAlcohol { get; set; }
        public string PrecioUnitarioReferencia { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public TablaImpuestoAdicionalModel TablaImpuestoAdicional { get; set; }
        public string MontoItem { get; set; }


    }

    public class TablaSubcantidadModel
    {
        public List<SubcantidadItemModel> SubcantidadItem { get; set; }
    }

    public class SubcantidadItemModel
    {
        public string Subcantidad { get; set; }
        public string CodigoSubcantidad { get; set; }
    }

    public class TablaImpuestoAdicionalModel
    {
        public List<ImpuestoAdicionalItemModel> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModel
    {
        public string TipoImpuesto { get; set; }
    }



}
