namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel11
    {
        public ECFModel11 ECF { get; set; } = new ECFModel11();
    }

    public class ECFModel11
    {
        public EncabezadoModel11 Encabezado { get; set; } = new EncabezadoModel11();
        public DetallesItemsModel11 DetallesItems { get; set; } = new DetallesItemsModel11();
        public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModel11
    {
        public string Version { get; set; }

        public VersionIdDocModel11 IdDoc { get; set; } = new VersionIdDocModel11();
        public EmisorModel11 Emisor { get; set; } = new EmisorModel11();
        //public CompradorModel11 Comprador { get; set; } = new CompradorModel11();
        public TotalesModel11 Totales { get; set; } = new TotalesModel11();
    }

    public class VersionIdDocModel11
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModel11
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

    public class CompradorModel11
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

    public class TotalesModel11
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI11 { get; set; }
        public string ITBIS11 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS11 { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }

    }

    public class DetallesItemsModel11
    {
        public List<ItemModel11> Item { get; set; } = new List<ItemModel11>();
    }

    public class ItemModel11
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string MontoItem { get; set; }

    }

}
