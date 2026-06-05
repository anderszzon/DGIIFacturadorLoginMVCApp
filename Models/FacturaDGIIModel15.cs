using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel15
    {
        public ECFModel15 ECF { get; set; } = new ECFModel15();
    }

    public class ECFModel15
    {
        public EncabezadoModel15 Encabezado { get; set; } = new EncabezadoModel15();
        public DetallesItemsModel15 DetallesItems { get; set; } = new DetallesItemsModel15();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Subtotales15 Subtotales { get; set; }

        public string FechaHoraFirma { get; set; }
    }

    public class InformacionReferencia15
    {
            public string NCFModificado { get; set; }
            public string FechaNCFModificado { get; set; }
            public string CodigoModificacion { get; set; }

    }

    public class EncabezadoModel15
    {
        public string Version { get; set; }

        public VersionIdDocModel15 IdDoc { get; set; } = new VersionIdDocModel15();
        public EmisorModel15 Emisor { get; set; } = new EmisorModel15();
        public CompradorModel15 Comprador { get; set; } = new CompradorModel15();

        public TotalesModel15 Totales { get; set; } = new TotalesModel15();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OtraMoneda15 OtraMoneda { get; set; }

    }

    public class VersionIdDocModel15
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string NumeroCuentaPago { get; set; }
        public string BancoPago { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }

        //public TablaFormasPago15 TablaFormasPago { get; set; } = new TablaFormasPago15();
        public string FechaLimitePago { get; set; }
        public string TerminoPago { get; set; }

    }

    public class TablaFormasPago15
    {
        public List<FormaDePago15> FormaDePago { get; set; }
    }

    public class FormaDePago15
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorModel15
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

    public class CompradorModel15
    {
        public string RNCComprador { get; set; }
        public string IdentificadorExtranjero { get; set; }
        public string RazonSocialComprador { get; set; }
        public string ContactoComprador { get; set; }
        public string CorreoComprador { get; set; }
        public string DireccionComprador { get; set; }
        public string MunicipioComprador { get; set; }
        public string ProvinciaComprador { get; set; }
        public string FechaEntrega { get; set; }

        public string ContactoEntrega { get; set; }

        public string DireccionEntrega { get; set; }

        public string TelefonoAdicional { get; set; }

        public string FechaOrdenCompra { get; set; }
        public string NumeroOrdenCompra { get; set; }
        public string CodigoInternoComprador { get; set; }
    }

    public class InformacionesAdicionales15
    {
        public string FechaEmbarque { get; set; }
        public string NumeroEmbarque { get; set; }
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }

        public string NombrePuertoEmbarque { get; set; }
        public string CondicionesEntrega { get; set; }
        public string TotalFob { get; set; }
        public string Seguro { get; set; }

        public string Flete { get; set; }
        public string TotalCif { get; set; }
        public string RegimenAduanero { get; set; }
        public string NombrePuertoSalida { get; set; }

        public string NombrePuertoDesembarque { get; set; }
        public string PesoBruto { get; set; }
        public string PesoNeto { get; set; }
        public string UnidadPesoBruto { get; set; }

        public string UnidadPesoNeto { get; set; }
        public string CantidadBulto { get; set; }
        public string UnidadBulto { get; set; }
        public string VolumenBulto { get; set; }
        public string UnidadVolumen { get; set; }

    }

    public class OtraMoneda15
    {
        public string TipoMoneda { get; set; }
        public string TipoCambio { get; set; }
        public string MontoExentoOtraMoneda { get; set; }
        public string MontoTotalOtraMoneda { get; set; }
    }

    public class TotalesModel15
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string MontoGravadoI2 { get; set; }
        public string MontoGravadoI3 { get; set; }
        public string ITBIS1 { get; set; }
        public string ITBIS2 { get; set; }
        public string ITBIS3 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string TotalITBIS2 { get; set; }
        public string TotalITBIS3 { get; set; }

        public string MontoImpuestoAdicional { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }
        
        public string MontoPeriodo { get; set; }
        public string ValorPagar { get; set; }
        public string TotalISRRetencion { get; set; }

    }

    public class DetallesItemsModel15
    {
        public List<ItemModel15> Item { get; set; } = new List<ItemModel15>();
    }
    public class Subtotales15
    {
        public List<Subtotal15> Subtotal { get; set; } = new List<Subtotal15>();
    }

    public class ItemModel15
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }

        public RetencionItem15 Retencion { get; set; }

        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public OtraMonedaDetalle15 OtraMonedaDetalle { get; set; }

        public string MontoItem { get; set; }

    }

    public class Subtotal15
    {
        public string NumeroSubTotal { get; set; }
        public string DescripcionSubtotal { get; set; }
        public string Orden { get; set; }
        public string SubTotalExento { get; set; }
        public string MontoSubTotal { get; set; }
        public string Lineas { get; set; }

    }

    public class RetencionItem15
    {
        public string IndicadorAgenteRetencionoPercepcion { get; set; }
        public string MontoISRRetenido { get; set; }

    }

    public class OtraMonedaDetalle15
    {
        public string PrecioOtraMoneda { get; set; }
        public string MontoItemOtraMoneda { get; set; }

    }

    public class TablaCodigosItem15
    {
        public List<CodigosItem15> CodigosItem { get; set; }
    }

    public class CodigosItem15
    {
        public string TipoCodigo { get; set; }
        public string CodigoItem { get; set; }

    }

    public class TablaSubDescuento15
    {
        public List<SubDescuento15> SubDescuento { get; set; }
    }

    public class TablaSubRecargo15
    {
        public List<SubRecargo15> SubRecargo { get; set; }
    }

    public class SubRecargo15
    {
        public string TipoSubRecargo { get; set; }
        public string MontoSubRecargo { get; set; }

    }

    public class SubDescuento15
    {
        public string TipoSubDescuento { get; set; }
        public string MontoSubDescuento { get; set; }

    }

}
