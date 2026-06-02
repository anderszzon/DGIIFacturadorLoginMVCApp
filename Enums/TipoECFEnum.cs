using System.ComponentModel.DataAnnotations;

namespace DGIIFacturadorLoginMVCApp.Enums
{
    public enum TipoECFEnum
    {
        [Display(Name = "Factura de Crédito Fiscal Electrónica")]
        Enum0 = 31,

        [Display(Name = "Factura de Consumo Electrónica")]
        Enum1 = 32,

        [Display(Name = "Nota de Débito Electrónica")]
        Enum2 = 33,

        [Display(Name = "Nota de Crédito Electrónica")]
        Enum3 = 34,

        [Display(Name = "Compras Electrónico")]
        Enum4 = 41,

        [Display(Name = "Gastos Menores Electrónico")]
        Enum5 = 43,

        [Display(Name = "Regímenes Especiales Electrónico")]
        Enum6 = 44,

        [Display(Name = "Gubernamental Electrónico ")]
        Enum7 = 45,

        [Display(Name = "Comprobante de Exportaciones Electrónico")]
        Enum8 = 46,

        [Display(Name = "Comprobante para Pagos al Exterior Electrónico")]
        Enum9 = 47
    }
}
