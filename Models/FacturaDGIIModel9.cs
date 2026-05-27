using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel9
    {
        public ECFModel9 ECF { get; set; } = new ECFModel9();
    }

    public class ECFModel9
    {
        public EncabezadoModel9 Encabezado { get; set; } = new EncabezadoModel9();
        public DetallesItemsModel9 DetallesItems { get; set; } = new DetallesItemsModel9();

        public InformacionReferencia9 InformacionReferencia { get; set; } = new InformacionReferencia9();
        public string FechaHoraFirma { get; set; }
    }

    public class InformacionReferencia9
    {
            public string NCFModificado { get; set; }
            public string FechaNCFModificado { get; set; }
            public string CodigoModificacion { get; set; }
            public string RazonModificacion { get; set; }

    }

    public class EncabezadoModel9
    {
        public string Version { get; set; }

        public VersionIdDocModel9 IdDoc { get; set; } = new VersionIdDocModel9();
        public EmisorModel9 Emisor { get; set; } = new EmisorModel9();
        public CompradorModel9 Comprador { get; set; } = new CompradorModel9();
        public InformacionesAdicionales9 InformacionesAdicionales { get; set; } = new InformacionesAdicionales9();
        public TotalesModel9 Totales { get; set; } = new TotalesModel9();
    }

    public class VersionIdDocModel9
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string IndicadorNotaCredito { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
        //public TablaFormasPago9 TablaFormasPago { get; set; } = new TablaFormasPago9();

    }

    public class TablaFormasPago9
    {
        public List<FormaDePago9> FormaDePago { get; set; }
    }

    public class FormaDePago9
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorModel9
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

    public class CompradorModel9
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

    public class InformacionesAdicionales9
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModel9
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
        public ImpuestosAdicionalesModel9 ImpuestosAdicionales { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }

        public string MontoNoFacturable { get; set; }

    }

    public class ImpuestosAdicionalesModel9
    {
        public List<ImpuestoAdicionalTotalesModel9> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel9
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel9
    {
        public List<ItemModel9> Item { get; set; } = new List<ItemModel9>();
    }

    public class ItemModel9
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string DescuentoMonto { get; set; }
        public TablaSubDescuento9 TablaSubDescuento { get; set; }
        public string RecargoMonto { get; set; }
        public TablaSubRecargo9 TablaSubRecargo { get; set; }
        public string MontoItem { get; set; }

    }

    public class TablaSubDescuento9
    {
        public List<SubDescuento9> SubDescuento { get; set; }
    }

    public class TablaSubRecargo9
    {
        public List<SubRecargo9> SubRecargo { get; set; }
    }

    public class SubRecargo9
    {
        public string TipoSubRecargo { get; set; }
        public string MontoSubRecargo { get; set; }

    }

    public class SubDescuento9
    {
        public string TipoSubDescuento { get; set; }
        public string MontoSubDescuento { get; set; }

    }

}
