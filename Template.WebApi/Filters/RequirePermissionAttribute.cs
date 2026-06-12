using Microsoft.AspNetCore.Mvc;

namespace Template.WebApi.Filters;

/// <summary>
/// 標示 Controller 或 Action 需要指定功能權限。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(params string[] permissionKeys)
        : base(typeof(RequirePermissionFilter))
    {
        PermissionKeys = permissionKeys;
        Arguments = [PermissionKeys];
    }

    public string[] PermissionKeys { get; }
}
