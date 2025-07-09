using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel5
    {
        public ECFModel5 ECF { get; set; } = new ECFModel5();
    }

    public class ECFModel5
    {
        public EncabezadoModel5 Encabezado { get; set; } = new EncabezadoModel5();
        public DetallesItemsModel5 DetallesItems { get; set; } = new DetallesItemsModel5();
        public DescuentosORecargosModel5 DescuentosORecargos { get; set; } = new DescuentosORecargosModel5();
        public string FechaHoraFirma { get; set; }
    }

    public class DescuentosORecargosModel5
    {
        public List<DescuentosORecargo5> DescuentoORecargo { get; set; }
    }

    public class DescuentosORecargo5
    {
        public string NumeroLinea { get; set; }
        public string TipoAjuste { get; set; }
        public string DescripcionDescuentooRecargo { get; set; }
        public string TipoValor { get; set; }
        public string MontoDescuentooRecargo { get; set; }
        public string IndicadorFacturacionDescuentooRecargo { get; set; }
    }


    public class EncabezadoModel5
    {
        public string Version { get; set; }

        public VersionIdDocModel5 IdDoc { get; set; } = new VersionIdDocModel5();
        public EmisorModel5 Emisor { get; set; } = new EmisorModel5();
        public CompradorModel5 Comprador { get; set; } = new CompradorModel5();
        public InformacionesAdicionales5 InformacionesAdicionales { get; set; } = new InformacionesAdicionales5();
        public TotalesModel5 Totales { get; set; } = new TotalesModel5();
    }

    public class VersionIdDocModel5
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
        public TablaFormasPago5 TablaFormasPago { get; set; } = new TablaFormasPago5();

    }

    public class TablaFormasPago5
    {
        public List<FormaDePago5> FormaDePago { get; set; }
    }

    public class FormaDePago5
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorModel5
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

    public class CompradorModel5
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

    public class InformacionesAdicionales5
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModel5
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
        public ImpuestosAdicionalesModel5 ImpuestosAdicionales { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }

    }

    public class ImpuestosAdicionalesModel5
    {
        public List<ImpuestoAdicionalTotalesModel5> ImpuestoAdicional { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel5
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel5
    {
        public List<ItemModel5> Item { get; set; } = new List<ItemModel5>();
    }

    public class ItemModel5
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string DescuentoMonto { get; set; }

        public TablaSubDescuento5 TablaSubDescuento { get; set; }

        public string MontoItem { get; set; }

    }

    public class TablaSubDescuento5
    {
        public List<SubDescuento5> SubDescuento { get; set; }
    }

    public class SubDescuento5
    {
        public string TipoSubDescuento { get; set; }
        public string MontoSubDescuento { get; set; }

    }

}
