namespace DGIIFacturadorLoginMVCApp.Services
{
    public class DgiiConfigService : IDgiiConfigService
    {
        public string Ambient { get; set; }

        public DgiiConfigService(IConfiguration configuration)
        {
            Ambient = configuration["DgiiConfig:DefaultAmbient"] ?? "certecf";
        }

        public string GetBaseUrl()
        {
            return $"https://ecf.dgii.gov.do/{Ambient}";
        }
    }
}
