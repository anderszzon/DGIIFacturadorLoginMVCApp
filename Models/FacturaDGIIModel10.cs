using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel10
    {
        public ECFModel10 ECF { get; set; } = new ECFModel10();
    }

    public class ECFModel10
    {
        public EncabezadoModel10 Encabezado { get; set; } = new EncabezadoModel10();
        public DetallesItemsModel10 DetallesItems { get; set; } = new DetallesItemsModel10();

        //public InformacionReferencia10 InformacionReferencia { get; set; } = new InformacionReferencia10();
        public string FechaHoraFirma { get; set; }
    }

    public class InformacionReferencia10
    {
            public string NCFModificado { get; set; }
            public string FechaNCFModificado { get; set; }
            public string CodigoModificacion { get; set; }

    }

    public class EncabezadoModel10
    {
        public string Version { get; set; }

        public VersionIdDocModel10 IdDoc { get; set; } = new VersionIdDocModel10();
        public EmisorModel10 Emisor { get; set; } = new EmisorModel10();
        public CompradorModel10 Comprador { get; set; } = new CompradorModel10();
        //public InformacionesAdicionales10 InformacionesAdicionales { get; set; } = new InformacionesAdicionales10();
        public TotalesModel10 Totales { get; set; } = new TotalesModel10();
    }

    public class VersionIdDocModel10
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
        public TablaFormasPago10 TablaFormasPago { get; set; } = new TablaFormasPago10();

    }

    public class TablaFormasPago10
    {
        public List<FormaDePago10> FormaDePago { get; set; }
    }

    public class FormaDePago10
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorModel10
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

    public class CompradorModel10
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

    public class InformacionesAdicionales10
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModel10
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
        public ImpuestosAdicionalesModel10 ImpuestosAdicionales { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }
        public string ValorPagar { get; set; }
        public string TotalITBISRetenido { get; set; }
        public string TotalISRRetencion { get; set; }


    }

    public class ImpuestosAdicionalesModel10
    {
        public List<ImpuestoAdicionalTotalesModel10> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel10
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel10
    {
        public List<ItemModel10> Item { get; set; } = new List<ItemModel10>();
    }

    public class ItemModel10
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public Retencion10 Retencion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string DescripcionItem { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string DescuentoMonto { get; set; }
        public TablaSubDescuento10 TablaSubDescuento { get; set; }
        public string RecargoMonto { get; set; }
        public TablaSubRecargo10 TablaSubRecargo { get; set; }
        public string MontoItem { get; set; }

    }

    public class Retencion10
    {
        public string IndicadorAgenteRetencionoPercepcion { get; set; }
        public string MontoITBISRetenido { get; set; }
        public string MontoISRRetenido { get; set; }
    }

    public class TablaSubDescuento10
    {
        public List<SubDescuento10> SubDescuento { get; set; }
    }

    public class TablaSubRecargo10
    {
        public List<SubRecargo10> SubRecargo { get; set; }
    }

    public class SubRecargo10
    {
        public string TipoSubRecargo { get; set; }
        public string MontoSubRecargo { get; set; }

    }

    public class SubDescuento10
    {
        public string TipoSubDescuento { get; set; }
        public string MontoSubDescuento { get; set; }

    }

}
