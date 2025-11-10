namespace KfConstructionWeb.Services.Interfaces;

public interface ISiteConfigService
{
    Task<string> GetCompanyNameAsync();
    Task<string> GetCompanyEmailAsync();
    Task<string> GetCompanyPhoneAsync();
    Task<string> GetCompanyAddressAsync();
    Task<string> GetSiteTitleAsync();
    Task<string> GetSiteDescriptionAsync();
    Task<string> GetPageTitleAsync(string pageTitle);
}
