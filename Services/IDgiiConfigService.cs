namespace DGIIFacturadorLoginMVCApp.Services
{
    public interface IDgiiConfigService
    {
        string Ambient { get; set; }
        string GetBaseUrl();
    }
}
