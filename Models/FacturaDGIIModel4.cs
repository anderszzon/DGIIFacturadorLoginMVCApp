using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel4
    {
        public ECFModel4 ECF { get; set; } = new ECFModel4();
    }

    public class ECFModel4
    {
        public EncabezadoModel4 Encabezado { get; set; } = new EncabezadoModel4();
        public DetallesItemsModel4 DetallesItems { get; set; } = new DetallesItemsModel4();
        public DescuentosORecargosModel4 DescuentosORecargos { get; set; } = new DescuentosORecargosModel4();
        public string FechaHoraFirma { get; set; }
    }

    public class DescuentosORecargosModel4
    {
        public List<DescuentosORecargo4> DescuentoORecargo { get; set; }
    }

    public class DescuentosORecargo4
    {
        public string NumeroLinea { get; set; }
        public string TipoAjuste { get; set; }
        public string DescripcionDescuentooRecargo { get; set; }
        public string TipoValor { get; set; }
        public string MontoDescuentooRecargo { get; set; }
        public string IndicadorFacturacionDescuentooRecargo { get; set; }
    }


    public class EncabezadoModel4
    {
        public string Version { get; set; }

        public VersionIdDocModel4 IdDoc { get; set; } = new VersionIdDocModel4();
        public EmisorModel4 Emisor { get; set; } = new EmisorModel4();
        public CompradorModel4 Comprador { get; set; } = new CompradorModel4();
        public InformacionesAdicionales4 InformacionesAdicionales { get; set; } = new InformacionesAdicionales4();
        public TotalesModel4 Totales { get; set; } = new TotalesModel4();
    }

    public class VersionIdDocModel4
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModel4
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

    public class CompradorModel4
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

    public class InformacionesAdicionales4
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModel4
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
        public ImpuestosAdicionalesModel4 ImpuestosAdicionales { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }

    }

    public class ImpuestosAdicionalesModel4
    {
        public List<ImpuestoAdicionalTotalesModel4> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel4
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel4
    {
        public List<ItemModel4> Item { get; set; } = new List<ItemModel4>();
    }

    public class ItemModel4
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }

        //public TablaImpuestoAdicionalModel4 TablaImpuestoAdicional { get; set; }

        public string MontoItem { get; set; }

    }

    public class TablaImpuestoAdicionalModel4
    {
        public List<ImpuestoAdicionalItemModel4> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalItemModel4
    {
        public string TipoImpuesto { get; set; }
    }

}
