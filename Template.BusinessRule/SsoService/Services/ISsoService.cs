using Template.BusinessRule.SsoService.Models;
using Template.Common.Models;

namespace Template.BusinessRule.SsoService.Services;

public interface ISsoService
{
    Task<PageListOutput<SsoClientDto>> GetClientsAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50);
    Task<SsoClientDto> CreateClientAsync(SsoClientCreateRequest request);
    Task<bool> UpdateClientAsync(SsoClientUpdateRequest request);
    Task<bool> DeleteClientAsync(int id);
    Task<SsoTokenResult> LoginAsync(string clientId, string clientSecret, string ip);
    Task<SsoTokenValidateResult> ValidateTokenAsync(string token);
}
