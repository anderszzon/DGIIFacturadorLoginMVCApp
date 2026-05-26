using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModelE32
    {
        public ECFModelE32 ECF { get; set; } = new ECFModelE32();
    }

    public class ECFModelE32
    {
        public EncabezadoModelE32 Encabezado { get; set; } = new EncabezadoModelE32();
        public DetallesItemsModelE32 DetallesItems { get; set; } = new DetallesItemsModelE32();
        public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModelE32
    {
        public string Version { get; set; }

        public VersionIdDocModelE32 IdDoc { get; set; } = new VersionIdDocModelE32();
        public EmisorModelE32 Emisor { get; set; } = new EmisorModelE32();
        public CompradorModelE32 Comprador { get; set; } = new CompradorModelE32();
        //public InformacionesAdicionalesE32 InformacionesAdicionales { get; set; } = new InformacionesAdicionalesE32();
        public TotalesModelE32 Totales { get; set; } = new TotalesModelE32();
    }

    public class VersionIdDocModelE32
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        //public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModelE32
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

    public class CompradorModelE32
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

    public class InformacionesAdicionalesE32
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModelE32
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

    public class ImpuestosAdicionalesModelE32
    {
        public List<ImpuestoAdicionalTotalesModelE32> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModelE32
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModelE32
    {
        public List<ItemModelE32> Item { get; set; } = new List<ItemModelE32>();
    }

    public class ItemModelE32
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

    public class TablaImpuestoAdicionalModelE32
    {
        public List<ImpuestoAdicionalItemModelE32> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModelE32
    {
        public string TipoImpuesto { get; set; }
    }

}
