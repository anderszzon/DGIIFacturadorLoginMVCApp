using System.ComponentModel.DataAnnotations;

namespace DGIIFacturadorLoginMVCApp.Enums
{
    public enum TipoIngresosEnum
    {
        [Display(Name = "Seleccione")]
        Enum0 = 0,

        [Display(Name = "Ingresos por operaciones (No Financieros)")]
        Enum1 = 1,

        [Display(Name = "Ingresos Financieros")]
        Enum2 = 2,

        [Display(Name = "Ingresos Extraordinarios")]
        Enum3 = 3,

        [Display(Name = "Ingresos por Arrendamientos")]
        Enum4 = 4,

        [Display(Name = "Ingresos por Venta de Activo Depreciable")]
        Enum5 = 5,

        [Display(Name = "Otros Ingresos")]
        Enum6 = 6
    }
}
