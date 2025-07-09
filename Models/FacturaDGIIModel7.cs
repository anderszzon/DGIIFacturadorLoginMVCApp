using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel7
    {
        public ECFModel7 ECF { get; set; } = new ECFModel7();
    }

    public class ECFModel7
    {
        public EncabezadoModel7 Encabezado { get; set; } = new EncabezadoModel7();
        public DetallesItemsModel7 DetallesItems { get; set; } = new DetallesItemsModel7();
        public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModel7
    {
        public string Version { get; set; }

        public VersionIdDocModel7 IdDoc { get; set; } = new VersionIdDocModel7();
        public EmisorModel7 Emisor { get; set; } = new EmisorModel7();
        public CompradorModel7 Comprador { get; set; } = new CompradorModel7();
        //public InformacionesAdicionales7 InformacionesAdicionales { get; set; } = new InformacionesAdicionales7();
        public TotalesModel7 Totales { get; set; } = new TotalesModel7();
    }

    public class VersionIdDocModel7
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModel7
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

    public class CompradorModel7
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

    //public class InformacionesAdicionales7
    //{
    //    public string NumeroContenedor { get; set; }
    //    public string NumeroReferencia { get; set; }
    //}
    public class TotalesModel7
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string MontoGravadoI2 { get; set; }
        public string MontoExento { get; set; }

        public string ITBIS1 { get; set; }
        public string ITBIS2 { get; set; }

        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string TotalITBIS2 { get; set; }

        public string MontoImpuestoAdicional { get; set; }
        public ImpuestosAdicionalesModel7 ImpuestosAdicionales { get; set; }
        public string MontoTotal { get; set; }
        public string ValorPagar { get; set; }

    }

    public class ImpuestosAdicionalesModel7
    {
        public List<ImpuestoAdicionalTotalesModel7> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel7
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel7
    {
        public List<ItemModel7> Item { get; set; } = new List<ItemModel7>();
    }

    public class ItemModel7
    {
        public string NumeroLinea { get; set; }
        public TablaCodigosItem7 TablaCodigosItem { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string RecargoMonto { get; set; }

        public TablaSubRecargo7 TablaSubRecargo { get; set; }

        public string MontoItem { get; set; }

    }

    public class TablaCodigosItem7
    {
        public List<CodigosItem7> CodigosItem { get; set; }
    }

    public class TablaSubRecargo7
    {
        public List<SubRecargo7> SubRecargo { get; set; }
    }

    public class SubRecargo7
    {
        public string TipoSubRecargo { get; set; }
        public string MontoSubRecargo { get; set; }

    }

    public class CodigosItem7
    {
        public string TipoCodigo { get; set; }
        public string CodigoItem { get; set; }

    }

    public class TablaImpuestoAdicionalModel7
    {
        public List<ImpuestoAdicionalItemModel7> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModel7
    {
        public string TipoImpuesto { get; set; }
    }

}
