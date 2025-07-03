using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel6
    {
        public ECFModel6 ECF { get; set; } = new ECFModel6();
    }

    public class ECFModel6
    {
        public EncabezadoModel6 Encabezado { get; set; } = new EncabezadoModel6();
        public DetallesItemsModel6 DetallesItems { get; set; } = new DetallesItemsModel6();
        public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModel6
    {
        public string Version { get; set; }

        public VersionIdDocModel6 IdDoc { get; set; } = new VersionIdDocModel6();
        public EmisorModel6 Emisor { get; set; } = new EmisorModel6();
        public CompradorModel6 Comprador { get; set; } = new CompradorModel6();
        public InformacionesAdicionales6 InformacionesAdicionales { get; set; } = new InformacionesAdicionales6();
        public TotalesModel6 Totales { get; set; } = new TotalesModel6();
    }

    public class VersionIdDocModel6
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModel6
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

    public class CompradorModel6
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

    public class InformacionesAdicionales6
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModel6
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string ITBIS1 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string MontoImpuestoAdicional { get; set; }
        public ImpuestosAdicionalesModel6 ImpuestosAdicionales { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }

    }

    public class ImpuestosAdicionalesModel6
    {
        public List<ImpuestoAdicionalTotalesModel6> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel6
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel6
    {
        public List<ItemModel6> Item { get; set; } = new List<ItemModel6>();
    }

    public class ItemModel6
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }

        //public TablaImpuestoAdicionalModel6 TablaImpuestoAdicional { get; set; }

        public string MontoItem { get; set; }

    }

    public class TablaImpuestoAdicionalModel6
    {
        public List<ImpuestoAdicionalItemModel6> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModel6
    {
        public string TipoImpuesto { get; set; }
    }

}
