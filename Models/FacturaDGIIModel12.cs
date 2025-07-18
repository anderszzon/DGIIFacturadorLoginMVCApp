using System.Text.Json.Serialization;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIModel12
    {
        public ECFModel12 ECF { get; set; } = new ECFModel12();
    }

    public class ECFModel12
    {
        public EncabezadoModel12 Encabezado { get; set; } = new EncabezadoModel12();
        public DetallesItemsModel12 DetallesItems { get; set; } = new DetallesItemsModel12();

        public DescuentosORecargosModel12 DescuentosORecargos { get; set; } = new DescuentosORecargosModel12();

        public string FechaHoraFirma { get; set; }
    }

    public class InformacionReferencia12
    {
            public string NCFModificado { get; set; }
            public string FechaNCFModificado { get; set; }
            public string CodigoModificacion { get; set; }

    }

    public class EncabezadoModel12
    {
        public string Version { get; set; }

        public VersionIdDocModel12 IdDoc { get; set; } = new VersionIdDocModel12();
        public EmisorModel12 Emisor { get; set; } = new EmisorModel12();
        public CompradorModel12 Comprador { get; set; } = new CompradorModel12();
        public TotalesModel12 Totales { get; set; } = new TotalesModel12();
    }

    public class VersionIdDocModel12
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
        public TablaFormasPago12 TablaFormasPago { get; set; } = new TablaFormasPago12();
        public string TipoCuentaPago { get; set; }
        public string NumeroCuentaPago { get; set; }
        public string BancoPago { get; set; }

    }

    public class TablaFormasPago12
    {
        public List<FormaDePago12> FormaDePago { get; set; }
    }

    public class FormaDePago12
    {
        public string FormaPago { get; set; }
        public string MontoPago { get; set; }
    }

    public class EmisorModel12
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

    public class CompradorModel12
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

    public class InformacionesAdicionales12
    {
        public string NumeroContenedor { get; set; }
        public string NumeroReferencia { get; set; }
    }
    public class TotalesModel12
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
        public ImpuestosAdicionalesModel12 ImpuestosAdicionales { get; set; }
        public string MontoExento { get; set; }
        public string MontoTotal { get; set; }
        public string ValorPagar { get; set; }


    }

    public class ImpuestosAdicionalesModel12
    {
        public List<ImpuestoAdicionalTotalesModel12> ImpuestoAdicional { get; set; }
    }

    public class DescuentosORecargosModel12
    {
        public List<DescuentosORecargo12> DescuentoORecargo { get; set; }
    }

    public class DescuentosORecargo12
    {
        public string NumeroLinea { get; set; }
        public string TipoAjuste { get; set; }
        public string DescripcionDescuentooRecargo { get; set; }
        public string TipoValor { get; set; }
        public string ValorDescuentooRecargo { get; set; }
        public string MontoDescuentooRecargo { get; set; }
        public string IndicadorFacturacionDescuentooRecargo { get; set; }
    }

    public class ImpuestoAdicionalTotalesModel12
    {
        public string TipoImpuesto { get; set; }
        public string TasaImpuestoAdicional { get; set; }
        public string OtrosImpuestosAdicionales { get; set; }
    }

    public class DetallesItemsModel12
    {
        public List<ItemModel12> Item { get; set; } = new List<ItemModel12>();
    }

    public class ItemModel12
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string DescuentoMonto { get; set; }
        public TablaSubDescuento12 TablaSubDescuento { get; set; }
        public string RecargoMonto { get; set; }
        public TablaSubRecargo12 TablaSubRecargo { get; set; }
        public string MontoItem { get; set; }

    }

    public class TablaSubDescuento12
    {
        public List<SubDescuento12> SubDescuento { get; set; }
    }

    public class TablaSubRecargo12
    {
        public List<SubRecargo12> SubRecargo { get; set; }
    }

    public class SubRecargo12
    {
        public string TipoSubRecargo { get; set; }
        public string MontoSubRecargo { get; set; }

    }

    public class SubDescuento12
    {
        public string TipoSubDescuento { get; set; }
        public string MontoSubDescuento { get; set; }

    }

}
