using Microsoft.AspNetCore.Authorization;

namespace Template.WebApi.Controllers;

[Authorize]
public abstract class AuthenticationController<T>(ILogger<T> logger) : BaseController<T>(logger)
{
}
