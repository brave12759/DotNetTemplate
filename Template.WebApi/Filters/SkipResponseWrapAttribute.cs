using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Template.Common.Models;

namespace Template.WebApi.Filters;

/// <summary>
/// 跳過全域回傳包裝
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipResponseWrapAttribute : Attribute { }
