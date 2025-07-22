using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel14
    {
        public ECFModel14 ECF { get; set; } = new ECFModel14();
    }

    public class ECFModel14
    {
        public EncabezadoModel14 Encabezado { get; set; } = new EncabezadoModel14();
        public DetallesItemsModel14 DetallesItems { get; set; } = new DetallesItemsModel14();

        public InformacionReferencia14 InformacionReferencia { get; set; } = new InformacionReferencia14();
        public string FechaHoraFirma { get; set; }
    }

    public class InformacionReferencia14
    {
            public string NCFModificado { get; set; }
            public string FechaNCFModificado { get; set; }
            public string CodigoModificacion { get; set; }

    }

    public class EncabezadoModel14
    {
        public string Version { get; set; }

        public VersionIdDocModel14 IdDoc { get; set; } = new VersionIdDocModel14();
        public EmisorModel14 Emisor { get; set; } = new EmisorModel14();
        public CompradorModel14 Comprador { get; set; } = new CompradorModel14();
        public InformacionesAdicionales14 InformacionesAdicionales { get; set; } = new InformacionesAdicionales14();

        public Transporte14 Transporte { get; set; } = new Transporte14();

        public TotalesModel14 Totales { get; set; } = new TotalesModel14();
    }

    public class VersionIdDocModel14
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
        public string FechaLimitePago { get; set; }
        public string TerminoPago { get; set; }
        public TablaFormasPago14 TablaFormasPago { get; set; } = new TablaFormasPago14();

    }

    public class TablaFormasPago14
    {
        public List<FormaDePago14> FormaDePago { get; set; }
    }

    public class FormaDePago14
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorModel14
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

    public class CompradorModel14
    {
        public string RNCComprador { get; set; }
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

    public class InformacionesAdicionales14
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

    public class Transporte14
    {
        public string ViaTransporte { get; set; }
        public string PaisOrigen { get; set; }

        public string DireccionDestino { get; set; }
        public string PaisDestino { get; set; }

        public string NumeroAlbaran { get; set; }
    }

    public class TotalesModel14
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

    }

    public class DetallesItemsModel14
    {
        public List<ItemModel14> Item { get; set; } = new List<ItemModel14>();
    }

    public class ItemModel14
    {
        public string NumeroLinea { get; set; }

        public TablaCodigosItem14 TablaCodigosItem { get; set; }

        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string MontoItem { get; set; }

    }

    public class TablaCodigosItem14
    {
        public List<CodigosItem14> CodigosItem { get; set; }
    }

    public class CodigosItem14
    {
        public string TipoCodigo { get; set; }
        public string CodigoItem { get; set; }

    }

    public class TablaSubDescuento14
    {
        public List<SubDescuento14> SubDescuento { get; set; }
    }

    public class TablaSubRecargo14
    {
        public List<SubRecargo14> SubRecargo { get; set; }
    }

    public class SubRecargo14
    {
        public string TipoSubRecargo { get; set; }
        public string MontoSubRecargo { get; set; }

    }

    public class SubDescuento14
    {
        public string TipoSubDescuento { get; set; }
        public string MontoSubDescuento { get; set; }

    }

}
