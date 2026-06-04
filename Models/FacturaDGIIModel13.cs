using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel13
    {
        public ECFModel13 ECF { get; set; } = new ECFModel13();
    }

    public class ECFModel13
    {
        public EncabezadoModel13 Encabezado { get; set; } = new EncabezadoModel13();
        public DetallesItemsModel13 DetallesItems { get; set; } = new DetallesItemsModel13();

        //public InformacionReferencia13 InformacionReferencia { get; set; } = new InformacionReferencia13();
        public string FechaHoraFirma { get; set; }
    }

    public class InformacionReferencia13
    {
            public string NCFModificado { get; set; }
            public string FechaNCFModificado { get; set; }
            public string CodigoModificacion { get; set; }
            public string RazonModificacion { get; set; }

    }

    public class EncabezadoModel13
    {
        public string Version { get; set; }

        public VersionIdDocModel13 IdDoc { get; set; } = new VersionIdDocModel13();
        public EmisorModel13 Emisor { get; set; } = new EmisorModel13();
        public CompradorModel13 Comprador { get; set; } = new CompradorModel13();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]

        public InformacionesAdicionales13 InformacionesAdicionales { get; set; }
        public TotalesModel13 Totales { get; set; } = new TotalesModel13();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OtraMoneda13 OtraMoneda { get; set; }

    }

    public class VersionIdDocModel13
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string IndicadorNotaCredito { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
        //public TablaFormasPago13 TablaFormasPago { get; set; } = new TablaFormasPago13();

    }

    public class TablaFormasPago13
    {
        public List<FormaDePago13> FormaDePago { get; set; }
    }

    public class FormaDePago13
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorModel13
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

    public class CompradorModel13
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

    public class InformacionesAdicionales13
    {
        public string FechaEmbarque { get; set; }
        public string NumeroEmbarque { get; set; }
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
        public string PesoBruto { get; set; }
        public string PesoNeto { get; set; }
        public string UnidadPesoBruto { get; set; }
        public string UnidadPesoNeto { get; set; }
        public string CantidadBulto { get; set; }
        public string UnidadBulto { get; set; }
        public string VolumenBulto { get; set; }
        public string UnidadVolumen { get; set; }

    }
    public class TotalesModel13
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
        public ImpuestosAdicionalesModel13 ImpuestosAdicionales { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }
        public string MontoPeriodo { get; set; }
        public string ValorPagar { get; set; }

    }

    public class ImpuestosAdicionalesModel13
    {
        public List<ImpuestoAdicionalTotalesModel13> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel13
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string MontoImpuestoSelectivoConsumoEspecifico { get; set; }
        public string MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel13
    {
        public List<ItemModel13> Item { get; set; } = new List<ItemModel13>();
    }

    public class OtraMoneda13
    {
        public string TipoMoneda { get; set; }
        public string TipoCambio { get; set; }
        public string MontoGravadoTotalOtraMoneda { get; set; }
        public string MontoGravado1OtraMoneda { get; set; }
        public string TotalITBISOtraMoneda { get; set; }
        public string TotalITBIS1OtraMoneda { get; set; }
        public string MontoTotalOtraMoneda { get; set; }
    }
    public class ItemModel13
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string DescripcionItem { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string CantidadReferencia { get; set; }
        public string UnidadReferencia { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TablaSubcantidadModel13 TablaSubcantidad { get; set; }
        public string GradosAlcohol { get; set; }
        public string PrecioUnitarioReferencia { get; set; }
        public string PrecioUnitarioItem { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TablaImpuestoAdicionalModel13 TablaImpuestoAdicional { get; set; }

        public OtraMonedaDetalle13 OtraMonedaDetalle { get; set; }


        public string DescuentoMonto { get; set; }
        public TablaSubDescuento13 TablaSubDescuento { get; set; }
        public string RecargoMonto { get; set; }
        public TablaSubRecargo13 TablaSubRecargo { get; set; }
        public string MontoItem { get; set; }

    }
    public class OtraMonedaDetalle13
    {
        public string PrecioOtraMoneda { get; set; }
        public string MontoItemOtraMoneda { get; set; }

    }
    public class TablaSubcantidadModel13
    {
        public List<SubcantidadItemModel13> SubcantidadItem { get; set; }
    }

    public class SubcantidadItemModel13
    {
        public string Subcantidad { get; set; }
        public string CodigoSubcantidad { get; set; }
    }

    public class TablaImpuestoAdicionalModel13
    {
        public List<ImpuestoAdicionalItemModel13> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModel13
    {
        public string TipoImpuesto { get; set; }
    }

    public class TablaSubDescuento13
    {
        public List<SubDescuento13> SubDescuento { get; set; }
    }

    public class TablaSubRecargo13
    {
        public List<SubRecargo13> SubRecargo { get; set; }
    }

    public class SubRecargo13
    {
        public string TipoSubRecargo { get; set; }
        public string MontoSubRecargo { get; set; }

    }

    public class SubDescuento13
    {
        public string TipoSubDescuento { get; set; }
        public string MontoSubDescuento { get; set; }

    }

}
