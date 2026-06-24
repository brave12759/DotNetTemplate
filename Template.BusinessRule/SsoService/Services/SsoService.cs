using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.PasswordManager.Services;
using Template.BusinessRule.SsoService.Enums;
using Template.BusinessRule.SsoService.Exceptions;
using Template.BusinessRule.SsoService.Models;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.SsoService.Services;

/// <summary>
/// SSO ??嚗?鞎祉恣??餃?祉頂蝯梁?憭蝟餌絞 client嚗蒂撽??祉頂蝯梁偷?潛? Server Token
/// </summary>
/// <remarks>
/// ?箸瘚??荔?蝟餌絞蝞∠??∪?撱箇? SSO client嚗? ClientId ??ClientSecret ?潛策憭蝟餌絞
/// 憭蝟餌絞?券?撣喳??澆 Login ???剜? Server Token
/// ?嗅隞頂蝯望?啗府 Token 敺??臬??ValidateToken 蝣箄? Token ?臬???舀蝟餌絞蝪賜??虫???嚗誑???? client ?臬隞???/// </remarks>
public class SsoService(IServiceProvider serviceProvider) : BaseService(serviceProvider), ISsoService
{
    private readonly Lazy<IPasswordManager> _passwordManager = new(() =>
        serviceProvider.GetRequiredService<IPasswordManager>());
    private readonly Lazy<IJwtService> _jwtService = new(() =>
        serviceProvider.GetRequiredService<IJwtService>());
    private readonly Lazy<ILogService?> _logService = new(() => serviceProvider.GetService<ILogService>());

    /// <inheritdoc />
    public async Task<PageListOutput<SsoClientDto>> GetClientsAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50)
    {
        if (enablePaging)
            PageListQueryableExtensions.ValidatePaging(page, pageSize);

        var query = Db.Sso_Clients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(c => c.ClientId.Contains(k) || c.ClientName.Contains(k));
        }

        if (isEnable.HasValue)
            query = query.Where(c => c.IsEnable == isEnable.Value);

        return await query
            .OrderBy(c => c.ClientId)
            .Select(ToDtoExpression())
            .ToPageListOutputAsync(page, pageSize, enablePaging);
    }

    /// <inheritdoc />
    public async Task<SsoClientDto> CreateClientAsync(SsoClientCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateClient(request.ClientId, request.ClientName);

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
            throw new SsoMessageException(SsoMessageEnum.ClientSecretRequired, nameof(request.ClientSecret));

        // ClientId 撠???嚗??銝嚗???函頂蝯梁?交??⊥??斗?臬??client
        var clientId = request.ClientId.Trim();
        if (await Db.Sso_Clients.AnyAsync(c => c.ClientId == clientId))
            throw new SsoMessageException(SsoMessageEnum.ClientIdAlreadyExists, nameof(request.ClientId));

        var now = DateTime.UtcNow;
        var entity = new Sso_Client
        {
            ClientId = clientId,
            ClientName = request.ClientName.Trim(),
            ClientSecretHash = _passwordManager.Value.HashForStorage(request.ClientSecret),
            IsEnable = request.IsEnable,
            CreatedTime = now,
            CreatedId = CurrentUser.UserId,
            UpdatedTime = now,
            UpdatedId = CurrentUser.UserId
        };

        // 撖怠鞈?摨怠?? DTO?TO 銝???ClientSecretHash嚗??secret 鞈?憭援
        Db.Sso_Clients.Add(entity);
        await Db.SaveChangesAsync();
        await WriteSsoClientOperationLogAsync(
            AuditActionEnum.Create,
            entity.ClientId,
            "建立 SSO Client。",
            newValue: MapToDto(entity));
        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateClientAsync(SsoClientUpdateRequest request)
    {
        // ?湔????摰????銝駁
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id <= 0)
            throw new SsoMessageException(SsoMessageEnum.IdMustBeGreaterThanZero, nameof(request.Id));

        // ClientName ?臬??啗??亦?迂嚗??迂蝛箇
        if (string.IsNullOrWhiteSpace(request.ClientName))
            throw new SsoMessageException(SsoMessageEnum.ClientNameRequired, nameof(request.ClientName));

        // ?曆??啗??誨銵刻◤?湔??client 銝??剁?? false 霈?Controller 頧? 404
        var entity = await Db.Sso_Clients.FirstOrDefaultAsync(c => c.Id == request.Id);
        if (entity is null)
            return false;

        var oldValue = MapToDto(entity);

        // ?湔憿舐內?迂?lientId 銝??暹?堆??踹?憭蝟餌絞?Ｘ??游?憭望?
        entity.ClientName = request.ClientName.Trim();

        // ClientSecret ??靘?頛芣撖Ⅳ嚗征?潔誨銵其?????secret
        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
            entity.ClientSecretHash = _passwordManager.Value.HashForStorage(request.ClientSecret);

        // IsEnable ?舐靘??冽????函頂蝯晞??典?銝?餃嚗??token 撽?銋?憭望?
        entity.IsEnable = request.IsEnable;
        entity.UpdatedTime = DateTime.UtcNow;
        entity.UpdatedId = CurrentUser.UserId;

        await Db.SaveChangesAsync();
        await WriteSsoClientOperationLogAsync(
            AuditActionEnum.Update,
            entity.ClientId,
            "更新 SSO Client。",
            oldValue,
            MapToDto(entity),
            new { SecretChanged = !string.IsNullOrWhiteSpace(request.ClientSecret) });
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteClientAsync(int id)
    {
        if (id <= 0)
            throw new SsoMessageException(SsoMessageEnum.IdMustBeGreaterThanZero, nameof(id));

        // ?曆??啗??誨銵典歇銝??剁?? false 霈?Controller 頧? 404
        var entity = await Db.Sso_Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null)
            return false;

        var oldValue = MapToDto(entity);

        // ?湔?芷 client?甇???啣??閬??里?貊????舀?? IsEnable
        Db.Sso_Clients.Remove(entity);
        await Db.SaveChangesAsync();
        await WriteSsoClientOperationLogAsync(
            AuditActionEnum.Delete,
            entity.ClientId,
            "刪除 SSO Client。",
            oldValue: oldValue);
        return true;
    }

    /// <inheritdoc />
    public async Task<SsoTokenResult> LoginAsync(string clientId, string clientSecret, string ip)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            await WriteSsoLogAsync(clientId, "Login", "Failure", ip, "SSO 登入失敗：ClientId 或 ClientSecret 未填寫。",
                new { Reason = "MissingCredential" });
            return SsoTokenResult.Fail(SsoMessageEnum.InvalidClientCredentials);
        }

        var client = await Db.Sso_Clients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == clientId.Trim());
        if (client is null || !client.IsEnable)
        {
            await WriteSsoLogAsync(clientId, "Login", "Failure", ip, "SSO 登入失敗：Client 不存在或已停用。",
                new { Reason = "ClientNotFoundOrDisabled" });
            return SsoTokenResult.Fail(SsoMessageEnum.InvalidClientCredentials);
        }

        if (!_passwordManager.Value.Verify(clientSecret, client.ClientSecretHash))
        {
            await WriteSsoLogAsync(client.ClientId, "Login", "Failure", ip, "SSO 登入失敗：ClientSecret 錯誤。",
                new { Reason = "WrongSecret" });
            return SsoTokenResult.Fail(SsoMessageEnum.InvalidClientCredentials);
        }

        var token = await _jwtService.Value.GenerateServerTokenAsync(client.ClientId, ip);
        await WriteSsoLogAsync(client.ClientId, "Login", "Success", ip, "SSO 登入成功並核發 Server Token。",
            new { Reason = "Success" });
        return SsoTokenResult.Ok(token);
    }

    /// <inheritdoc />
    public async Task<SsoTokenResult> RefreshAsync(string token, string ip)
    {
        var principal = await _jwtService.Value.ValidateExpiredTokenAsync(token);
        if (principal is null)
        {
            await WriteSsoLogAsync(string.Empty, "RefreshToken", "Failure", ip, "SSO Token 刷新失敗：Token 無效。",
                new { Reason = "InvalidToken" });
            return SsoTokenResult.Fail(SsoMessageEnum.InvalidClientCredentials);
        }

        var tokenType = principal.FindFirst("token_type")?.Value;
        var clientId = principal.FindFirst("client_id")?.Value ?? principal.Identity?.Name ?? string.Empty;
        var clientEnabled = await Db.Sso_Clients
            .AsNoTracking()
            .AnyAsync(c => c.ClientId == clientId && c.IsEnable);

        if (tokenType != "server" || string.IsNullOrWhiteSpace(clientId) || !clientEnabled)
        {
            await WriteSsoLogAsync(clientId, "RefreshToken", "Failure", ip, "SSO Token 刷新失敗：Token 類型或 Client 狀態不符。",
                new { Reason = "ClientTypeOrStateMismatch", TokenType = tokenType, ClientEnabled = clientEnabled });
            return SsoTokenResult.Fail(SsoMessageEnum.InvalidClientCredentials);
        }

        var newToken = await _jwtService.Value.GenerateServerTokenAsync(clientId, ip);
        await WriteSsoLogAsync(clientId, "RefreshToken", "Success", ip, "SSO Token 刷新成功。",
            new { Reason = "Success" });
        return SsoTokenResult.Ok(newToken);
    }

    /// <inheritdoc />
    public async Task<SsoTokenValidateResult> ValidateTokenAsync(string token)
    {
        var principal = await _jwtService.Value.ValidateTokenAsync(token);
        if (principal is null)
        {
            await WriteSsoLogAsync(string.Empty, "ValidateToken", "Failure", string.Empty, "SSO Token 驗證失敗。",
                new { Reason = "InvalidToken" });
            return new SsoTokenValidateResult { IsValid = false };
        }

        var tokenType = principal.FindFirst("token_type")?.Value;
        var clientId = principal.FindFirst("client_id")?.Value ?? principal.Identity?.Name ?? string.Empty;
        var clientEnabled = await Db.Sso_Clients
            .AsNoTracking()
            .AnyAsync(c => c.ClientId == clientId && c.IsEnable);

        var exp = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        var expiresAt = long.TryParse(exp, out var expUnix)
            ? DateTimeOffset.FromUnixTimeSeconds(expUnix)
            : (DateTimeOffset?)null;

        var isValid = tokenType == "server" && !string.IsNullOrWhiteSpace(clientId) && clientEnabled;
        await WriteSsoLogAsync(
            clientId,
            "ValidateToken",
            isValid ? "Success" : "Failure",
            string.Empty,
            isValid ? "SSO Token 驗證成功。" : "SSO Token 驗證失敗：Token 類型或 Client 狀態不符。",
            new { Reason = isValid ? "Success" : "ClientTypeOrStateMismatch", TokenType = tokenType, ClientEnabled = clientEnabled });

        return new SsoTokenValidateResult
        {
            IsValid = isValid,
            ClientId = clientId,
            ExpiresAt = expiresAt
        };
    }
    /// <summary>
    /// 瑼Ｘ SSO client ???亥???血??湛?ClientSecret ?勗遣蝡?蝔憭炎??    /// </summary>
    private static void ValidateClient(string clientId, string clientName)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new SsoMessageException(SsoMessageEnum.ClientIdRequired, nameof(clientId));

        if (string.IsNullOrWhiteSpace(clientName))
            throw new SsoMessageException(SsoMessageEnum.ClientNameRequired, nameof(clientName));
    }

    /// <summary>
    /// 撠?SSO client 鞈?銵典祕擃??撓??DTO嚗?頛詨 secret hash
    /// </summary>
    private static SsoClientDto MapToDto(Sso_Client client)
    {
        return new SsoClientDto
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            IsEnable = client.IsEnable,
            CreatedTime = client.CreatedTime,
            CreatedId = client.CreatedId,
            UpdatedTime = client.UpdatedTime,
            UpdatedId = client.UpdatedId
        };
    }

    /// <summary>
    /// 寫入 SSO Client 管理操作日誌；不記錄 ClientSecret 或雜湊值。
    /// </summary>
    private Task WriteSsoClientOperationLogAsync(
        AuditActionEnum action,
        string clientId,
        string message,
        object? oldValue = null,
        object? newValue = null,
        object? metadata = null)
    {
        return _logService.Value?.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "SSO",
            Action = action,
            Result = AuditResultEnum.Success,
            TargetType = nameof(Sso_Client),
            TargetId = clientId,
            Message = message,
            OldValue = oldValue,
            NewValue = newValue,
            Metadata = metadata
        }) ?? Task.CompletedTask;
    }

    /// <summary>
    /// 寫入 SSO 串接日誌；不記錄 Server Token 或 ClientSecret。
    /// </summary>
    private Task WriteSsoLogAsync(
        string clientId,
        string eventName,
        string result,
        string ip,
        string message,
        object? metadata = null)
    {
        var normalizedClientId = clientId.Trim();
        return _logService.Value?.WriteSsoAsync(new SsoLogCreateRequest
        {
            OperatorId = normalizedClientId,
            ClientId = normalizedClientId,
            EventName = eventName,
            Result = result,
            IpAddress = ip,
            Message = message,
            Metadata = metadata
        }) ?? Task.CompletedTask;
    }

    /// <summary>
    /// 撱箇? EF Core ?亥岷?蔣嚗? client 皜?亥岷?湔?刻??澈蝡舫??DTO 甈?
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<Sso_Client, SsoClientDto>> ToDtoExpression()
    {
        return client => new SsoClientDto
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            IsEnable = client.IsEnable,
            CreatedTime = client.CreatedTime,
            CreatedId = client.CreatedId,
            UpdatedTime = client.UpdatedTime,
            UpdatedId = client.UpdatedId
        };
    }
}


