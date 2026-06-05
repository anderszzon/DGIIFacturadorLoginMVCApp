using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel8
    {
        public ECFModel8 ECF { get; set; } = new ECFModel8();
    }

    public class ECFModel8
    {
        public EncabezadoModel8 Encabezado { get; set; } = new EncabezadoModel8();
        public DetallesItemsModel8 DetallesItems { get; set; } = new DetallesItemsModel8();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InformacionReferencia8 InformacionReferencia { get; set; }
        public string FechaHoraFirma { get; set; }
    }

    public class InformacionReferencia8
    {
            public string NCFModificado { get; set; }
            public string FechaNCFModificado { get; set; }
            public string CodigoModificacion { get; set; }

    }

    public class EncabezadoModel8
    {
        public string Version { get; set; }

        public VersionIdDocModel8 IdDoc { get; set; } = new VersionIdDocModel8();
        public EmisorModel8 Emisor { get; set; } = new EmisorModel8();
        public CompradorModel8 Comprador { get; set; } = new CompradorModel8();
        public InformacionesAdicionales8 InformacionesAdicionales { get; set; } = new InformacionesAdicionales8();
        public TotalesModel8 Totales { get; set; } = new TotalesModel8();
    }

    public class VersionIdDocModel8
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
        public TablaFormasPago8 TablaFormasPago { get; set; } = new TablaFormasPago8();

    }

    public class TablaFormasPago8
    {
        public List<FormaDePago8> FormaDePago { get; set; }
    }

    public class FormaDePago8
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorModel8
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

    public class CompradorModel8
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

    public class InformacionesAdicionales8
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModel8
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string MontoGravadoI2 { get; set; }
        public string ITBIS1 { get; set; }
        public string ITBIS2 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string TotalITBIS2 { get; set; }
        public string MontoImpuestoAdicional { get; set; }
        public ImpuestosAdicionalesModel8 ImpuestosAdicionales { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }

    }

    public class ImpuestosAdicionalesModel8
    {
        public List<ImpuestoAdicionalTotalesModel8> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel8
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel8
    {
        public List<ItemModel8> Item { get; set; } = new List<ItemModel8>();
    }

    public class ItemModel8
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string DescuentoMonto { get; set; }
        public TablaSubDescuento8 TablaSubDescuento { get; set; }
        public string RecargoMonto { get; set; }
        public TablaSubRecargo8 TablaSubRecargo { get; set; }
        public string MontoItem { get; set; }

    }

    public class TablaSubDescuento8
    {
        public List<SubDescuento8> SubDescuento { get; set; }
    }

    public class TablaSubRecargo8
    {
        public List<SubRecargo8> SubRecargo { get; set; }
    }

    public class SubRecargo8
    {
        public string TipoSubRecargo { get; set; }
        public string MontoSubRecargo { get; set; }

    }

    public class SubDescuento8
    {
        public string TipoSubDescuento { get; set; }
        public string MontoSubDescuento { get; set; }

    }

}
