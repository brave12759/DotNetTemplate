using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Template.BusinessRule.SsoService.Enums;
using Template.BusinessRule.SsoService.Exceptions;
using Template.BusinessRule.SsoService.Models;
using Template.BusinessRule.SsoService.Services;
using Template.Common.Extensions;
using Template.WebApi.Filters;
using Template.WebApi.Models.Auth;

namespace Template.WebApi.Controllers;

public class SsoController(
    ILogger<SsoController> logger,
    ISsoService ssoService) : AuthenticationController<SsoController>(logger)
{
    private const string ManageSsoClientsPermission = "System.SsoClient:Manage";

    [HttpGet]
    [RequirePermission(ManageSsoClientsPermission)]
    public async Task<IActionResult> Clients([FromQuery] string? keyword, [FromQuery] bool? isEnable)
    {
        var clients = await ssoService.GetClientsAsync(keyword, isEnable);
        return Ok(clients);
    }

    [HttpPost]
    [RequirePermission(ManageSsoClientsPermission)]
    public async Task<IActionResult> CreateClient([FromBody] SsoClientCreateRequest request)
    {
        try
        {
            var client = await ssoService.CreateClientAsync(request);
            return Ok(client);
        }
        catch (SsoMessageException ex)
        {
            return BadRequest(ToMessageResponse(ex.MessageCode));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut]
    [RequirePermission(ManageSsoClientsPermission)]
    public async Task<IActionResult> UpdateClient([FromBody] SsoClientUpdateRequest request)
    {
        try
        {
            var updated = await ssoService.UpdateClientAsync(request);
            if (!updated)
                return NotFound(ToMessageResponse(SsoMessageEnum.ClientNotFound));

            return Ok(ToMessageResponse(SsoMessageEnum.UpdatedSuccessfully));
        }
        catch (SsoMessageException ex)
        {
            return BadRequest(ToMessageResponse(ex.MessageCode));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch]
    [RequirePermission(ManageSsoClientsPermission)]
    public async Task<IActionResult> PatchClient([FromRoute] int id, [FromBody] JsonPatchDocument<SsoClientUpdateRequest> patch)
    {
        try
        {
            var client = await ssoService.GetClientByIdAsync(id);
            if (client is null)
                return NotFound(ToMessageResponse(SsoMessageEnum.ClientNotFound));

            var request = new SsoClientUpdateRequest
            {
                Id = client.Id,
                ClientName = client.ClientName,
                ClientSecret = null,
                IsEnable = client.IsEnable
            };

            patch.ApplyTo(request, error => ModelState.AddModelError(error.Operation.path, error.ErrorMessage));
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            request.Id = id;
            var updated = await ssoService.UpdateClientAsync(request);
            if (!updated)
                return NotFound(ToMessageResponse(SsoMessageEnum.ClientNotFound));

            return Ok(ToMessageResponse(SsoMessageEnum.UpdatedSuccessfully));
        }
        catch (SsoMessageException ex)
        {
            return BadRequest(ToMessageResponse(ex.MessageCode));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete]
    [RequirePermission(ManageSsoClientsPermission)]
    public async Task<IActionResult> DeleteClient([FromRoute] int id)
    {
        try
        {
            var deleted = await ssoService.DeleteClientAsync(id);
            if (!deleted)
                return NotFound(ToMessageResponse(SsoMessageEnum.ClientNotFound));

            return Ok(ToMessageResponse(SsoMessageEnum.DeletedSuccessfully));
        }
        catch (SsoMessageException ex)
        {
            return BadRequest(ToMessageResponse(ex.MessageCode));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] SsoLoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var result = await ssoService.LoginAsync(request.ClientId, request.ClientSecret, ip);
        if (!result.Success)
            return Unauthorized(ToMessageResponse(result.MessageCode ?? SsoMessageEnum.InvalidClientCredentials));

        return Ok(new { result.Token });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Refresh([FromBody] TokenValidateRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var result = await ssoService.RefreshAsync(request.Token, ip);
        if (!result.Success)
            return Unauthorized(ToMessageResponse(result.MessageCode ?? SsoMessageEnum.InvalidClientCredentials));

        return Ok(new { result.Token });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ValidateToken([FromBody] TokenValidateRequest request)
    {
        var result = await ssoService.ValidateTokenAsync(request.Token);
        return Ok(result);
    }

    /// <summary>
    /// 將 SSO 訊息 enum 轉成 API 統一回傳格式，讓前端可用 Code 判斷流程，也能直接顯示中文 Message。
    /// </summary>
    private static object ToMessageResponse(SsoMessageEnum code) => new
    {
        Code = code.ToInt(),
        Name = code.ToString(),
        Message = code.GetDescription()
    };
}
