using DGIIFacturadorLoginMVCApp.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DGIIFacturadorLoginMVCApp.Helpers
{
    public static class EnumHelper
    {
        public static List<SelectListItem> ObtenerTipoIngresos()
        {
            return Enum.GetValues(typeof(TipoIngresosEnum))
                .Cast<TipoIngresosEnum>()
                .Select(e => new SelectListItem
                {
                    Value = (int)e == 0
                        ? ""
                        : ((int)e).ToString("D2"),

                    Text = e.GetType()
                            .GetMember(e.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?
                            .GetName() ?? e.ToString()
                })
                .ToList();
        }
    }
}
