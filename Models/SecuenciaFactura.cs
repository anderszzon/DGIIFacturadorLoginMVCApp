using System.ComponentModel.DataAnnotations;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class SecuenciaFactura
    {
        [Key]
        public int Id { get; set; }
        public string Prefijo { get; set; } = "MTP";
        public int UltimoNumero { get; set; } = 0;
    }
}