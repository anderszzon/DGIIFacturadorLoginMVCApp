using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel2
    {
        public ECFModel2 ECF { get; set; } = new ECFModel2();
    }

    public class ECFModel2
    {
        public EncabezadoModel2 Encabezado { get; set; } = new EncabezadoModel2();
        public DetallesItemsModel2 DetallesItems { get; set; } = new DetallesItemsModel2();
        public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModel2
    {
        public string Version { get; set; }

        public VersionIdDocModel2 IdDoc { get; set; } = new VersionIdDocModel2();
        public EmisorModel2 Emisor { get; set; } = new EmisorModel2();
        public CompradorModel2 Comprador { get; set; } = new CompradorModel2();
        public TotalesModel2 Totales { get; set; } = new TotalesModel2();
    }

    public class VersionIdDocModel2
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModel2
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

    public class CompradorModel2
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

    public class TotalesModel2
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string ITBIS1 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string MontoImpuestoAdicional { get; set; }
        public ImpuestosAdicionalesModel2 ImpuestosAdicionales { get; set; }
        public string MontoTotal { get; set; }

    }

    public class ImpuestosAdicionalesModel2
    {
        public List<ImpuestoAdicionalTotalesModel2> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel2
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MontoImpuestoSelectivoConsumoEspecifico { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
    }

    public class DetallesItemsModel2
    {
        public List<ItemModel2> Item { get; set; } = new List<ItemModel2>();
    }

    public class ItemModel2
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string CantidadReferencia { get; set; }
        public string UnidadReferencia { get; set; }
        public TablaSubcantidadModel2 TablaSubcantidad { get; set; }
        public string GradosAlcohol { get; set; }
        public string PrecioUnitarioReferencia { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public TablaImpuestoAdicionalModel2 TablaImpuestoAdicional { get; set; }
        public string MontoItem { get; set; }


    }

    public class TablaSubcantidadModel2
    {
        public List<SubcantidadItemModel2> SubcantidadItem { get; set; }
    }

    public class SubcantidadItemModel2
    {
        public string Subcantidad { get; set; }
        public string CodigoSubcantidad { get; set; }
    }

    public class TablaImpuestoAdicionalModel2
    {
        public List<ImpuestoAdicionalItemModel2> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModel2
    {
        public string TipoImpuesto { get; set; }
    }



}
