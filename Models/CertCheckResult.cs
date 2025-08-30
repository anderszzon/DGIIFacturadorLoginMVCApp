using System.ComponentModel.DataAnnotations;

namespace DGIIFacturadorLoginMVCApp.Models
{
    public class CertCheckResult
    {
        public bool Existe { get; set; }
        public string Mensaje { get; set; }
        public string Subject { get; set; }
        public string Thumbprint { get; set; }
    }

}
