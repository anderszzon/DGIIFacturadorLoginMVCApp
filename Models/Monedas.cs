using System.ComponentModel.DataAnnotations;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class Monedas
    {
        [Key]
        public int IdMoneda { get; set; }
        public string? Moneda { get; set; }
        public int? TipoCambio { get; set; }
        public bool? Activo { get; set; }

    }

}
