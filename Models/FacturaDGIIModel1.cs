namespace DGIIFacturadorLoginMVCApp.Models
{
    public class FacturaDGIIResponseModel1
    {
        public string JsonInvoice { get; set; }
        public string ENCF { get; set; }
        public string XmlSemilla { get; set; }
        public string XmlSemillaFirmada { get; set; }
        public string Token { get; set; }
        public string XmlFactura { get; set; }
        public string XmlFacturaFirmada { get; set; }
        public string CodigoSeguridad { get; set; }
        public string CodigoRespuesta { get; set; }
        public string EstadoRespuesta { get; set; }
        public string Mensaje { get; set; }

    }

    public class FacturaDGIIModel1
    {
        public ECFModel1 ECF { get; set; } = new ECFModel1();
    }

    public class ECFModel1
    {
        public EncabezadoModel1 Encabezado { get; set; } = new EncabezadoModel1();
        public DetallesItemsModel1 DetallesItems { get; set; } = new DetallesItemsModel1();
        public string FechaHoraFirma { get; set; }
    }

    public class EncabezadoModel1
    {
        public string Version { get; set; }

        public VersionIdDocModel1 IdDoc { get; set; } = new VersionIdDocModel1();
        public EmisorModel1 Emisor { get; set; } = new EmisorModel1();
        public CompradorModel1 Comprador { get; set; } = new CompradorModel1();
        public TotalesModel1 Totales { get; set; } = new TotalesModel1();
    }

    public class VersionIdDocModel1
    {
        public string TipoeCF { get; set; }
        public string eNCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public string IndicadorEnvioDiferido { get; set; }
        public string IndicadorMontoGravado { get; set; }
        public string TipoIngresos { get; set; }
        public string TipoPago { get; set; }
    }

    public class EmisorModel1
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

    public class CompradorModel1
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

    public class TotalesModel1
    {
        public string MontoGravadoTotal { get; set; }
        public string MontoGravadoI1 { get; set; }
        public string ITBIS1 { get; set; }
        public string TotalITBIS { get; set; }
        public string TotalITBIS1 { get; set; }
        public string MontoTotal { get; set; }


        //public string MontoImpuestoAdicional { get; set; }
        //public ImpuestosAdicionalesModel1 ImpuestosAdicionales { get; set; }

    }

    //public class ImpuestosAdicionalesModel1
    //{
    //    public List<ImpuestoAdicionalTotalesModel1> ImpuestoAdicional { get; set; }
    //}

    //public class ImpuestoAdicionalTotalesModel1
    //{
    //    public string TipoImpuesto { get; set; }
    //    public string TasaImpuestoAdicional { get; set; }
    //    public string MontoImpuestoSelectivoConsumoEspecifico { get; set; }
    //    public string MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
    //}

    public class DetallesItemsModel1
    {
        public List<ItemModel1> Item { get; set; } = new List<ItemModel1>();
    }

    public class ItemModel1
    {
        public string NumeroLinea { get; set; }
        public string IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public string IndicadorBienoServicio { get; set; }
        public string CantidadItem { get; set; }
        public string UnidadMedida { get; set; }
        public string PrecioUnitarioItem { get; set; }
        public string MontoItem { get; set; }


        //public string CantidadReferencia { get; set; }
        //public string UnidadReferencia { get; set; }
        //public TablaSubcantidadModel1 TablaSubcantidad { get; set; }
        //public string GradosAlcohol { get; set; }
        //public string PrecioUnitarioReferencia { get; set; }
        //public TablaImpuestoAdicionalModel1 TablaImpuestoAdicional { get; set; }

    }

    //public class TablaSubcantidadModel1
    //{
    //    public List<SubcantidadItemModel1> SubcantidadItem { get; set; }
    //}

    //public class SubcantidadItemModel1
    //{
    //    public string Subcantidad { get; set; }
    //    public string CodigoSubcantidad { get; set; }
    //}

    //public class TablaImpuestoAdicionalModel1
    //{
    //    public List<ImpuestoAdicionalItemModel1> ImpuestoAdicional { get; set; }
    //}

    //public class ImpuestoAdicionalItemModel1
    //{
    //    public string TipoImpuesto { get; set; }
    //}



}
