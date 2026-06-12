using Microsoft.AspNetCore.Mvc;
using Template.Common.Models.Jwt;
using Template.Common.Services;
using Template.WebApi.Filters;

namespace Template.WebApi.Controllers;

/// <summary>
/// 提供 JWT 參數查詢與更新 API。
/// </summary>
public class JwtSettingController(
    ILogger<JwtSettingController> logger,
    IJwtService jwtService,
    ICurrentUserService currentUserService) : AuthenticationController<JwtSettingController>(logger)
{
    private const string ManageJwtSettingsPermission = "System.JwtSetting:Manage";

    /// <summary>
    /// 取得目前 JWT 設定（SecretKey 會遮罩）。
    /// </summary>
    /// <returns>JWT 設定資料。</returns>
    [HttpGet]
    [RequirePermission(ManageJwtSettingsPermission)]
    public async Task<IActionResult> Get()
    {
        try
        {
            var settings = await jwtService.GetSettingsAsync();
            return Ok(new
            {
                SecretKey = MaskSecretKey(settings.SecretKey),
                settings.Issuer,
                settings.Audience,
                settings.PersonalTokenExpire,
                settings.ServerTokenExpire
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新 JWT 設定。
    /// </summary>
    /// <param name="request">JWT 設定更新內容。</param>
    /// <returns>更新結果。</returns>
    [HttpPut]
    [RequirePermission(ManageJwtSettingsPermission)]
    public async Task<IActionResult> Update([FromBody] JwtSettingUpdateRequest request)
    {
        try
        {
            await jwtService.UpdateSettingsAsync(request, currentUserService.CurrentUser.UserId);
            return Ok(new { Message = "Updated successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 回傳 JWT 設定時遮罩簽章金鑰，避免完整金鑰外洩。
    /// </summary>
    private static string MaskSecretKey(string secretKey)
    {
        if (string.IsNullOrEmpty(secretKey))
            return string.Empty;

        return secretKey.Length <= 8
            ? new string('*', secretKey.Length)
            : string.Concat(secretKey.AsSpan(0, 4), new string('*', secretKey.Length - 8), secretKey.AsSpan(secretKey.Length - 4));
    }
}
