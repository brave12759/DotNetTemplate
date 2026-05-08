using Microsoft.AspNetCore.Mvc;

namespace Template.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public abstract class BaseController<T>(ILogger<T> logger) : ControllerBase
{
    protected readonly ILogger<T> Logger = logger;
}
