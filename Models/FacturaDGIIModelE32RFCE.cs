using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModelE32RFCE
    {
        public ECFModelE32RFCE RFCE { get; set; } = new ECFModelE32RFCE();
    }

    public class ECFModelE32RFCE
    {
        public EncabezadoModelE32RFCE Encabezado { get; set; } = new EncabezadoModelE32RFCE();
        //public DetallesItemsModelE32RFCE DetallesItems { get; set; } = new DetallesItemsModelE32RFCE();
        //public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModelE32RFCE
    {
        public string Version { get; set; }

        public VersionIdDocModelE32RFCE IdDoc { get; set; } = new VersionIdDocModelE32RFCE();
        public EmisorModelE32RFCE Emisor { get; set; } = new EmisorModelE32RFCE();
        public CompradorModelE32RFCE Comprador { get; set; } = new CompradorModelE32RFCE();
        //public InformacionesAdicionalesE32 InformacionesAdicionales { get; set; } = new InformacionesAdicionalesE32();
        public TotalesModelE32RFCE Totales { get; set; } = new TotalesModelE32RFCE();
    }

    public class VersionIdDocModelE32RFCE
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        //public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        //public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModelE32RFCE
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

    public class CompradorModelE32RFCE
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

    public class InformacionesAdicionalesE32RFCE
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModelE32RFCE
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string MontoGravadoI2 { get; set; }
        public string MontoGravadoI3 { get; set; }
        public string MontoExento { get; set; }

        public string ITBIS1 { get; set; }
        public string ITBIS2 { get; set; }
        public string ITBIS3 { get; set; }

        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string TotalITBIS2 { get; set; }
        public string TotalITBIS3 { get; set; }

        //public string MontoImpuestoAdicional { get; set; }
        //public ImpuestosAdicionalesModelE32 ImpuestosAdicionales { get; set; }
        public string MontoTotal { get; set; }
        public string MontoPeriodo { get; set; }
        public string ValorPagar { get; set; }

    }

    public class ImpuestosAdicionalesModelE32RFCE
    {
        public List<ImpuestoAdicionalTotalesModelE32RFCE> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModelE32RFCE
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModelE32RFCE
    {
        public List<ItemModelE32RFCE> Item { get; set; } = new List<ItemModelE32RFCE>();
    }

    public class ItemModelE32RFCE
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }

        //public TablaImpuestoAdicionalModelE32 TablaImpuestoAdicional { get; set; }

        public string MontoItem { get; set; }

    }

    public class TablaImpuestoAdicionalModelE32RFCE
    {
        public List<ImpuestoAdicionalItemModelE32RFCE> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModelE32RFCE
    {
        public string TipoImpuesto { get; set; }
    }

}
