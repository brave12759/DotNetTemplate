using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Template.WebApi.Routing;

/// <summary>
/// 依 action 命名慣例集中產生 REST 風格路由。
/// </summary>
public sealed class RestfulRouteConvention : IActionModelConvention
{
    private static readonly string[] CollectionPrefixes = ["Create", "Update", "Delete", "Patch"];

    /// <inheritdoc />
    public void Apply(ActionModel action)
    {
        var template = ResolveTemplate(action);
        foreach (var selector in action.Selectors)
        {
            selector.AttributeRouteModel ??= new AttributeRouteModel();
            selector.AttributeRouteModel.Template = template;
        }
    }

    private static string? ResolveTemplate(ActionModel action)
    {
        if (TryBuildNestedResourceTemplate(action, out var nestedTemplate))
            return nestedTemplate;

        if (TryBuildCollectionTemplate(action, out var collectionTemplate))
            return collectionTemplate;

        if (action.ActionName is "List" or "Create" or "Update" or "Get")
            return null;

        if (action.ActionName is "GetById" or "Delete" or "Patch")
            return BuildRouteParameterTemplate(action);

        return ToKebabCase(action.ActionName);
    }

    private static bool TryBuildNestedResourceTemplate(ActionModel action, out string template)
    {
        template = string.Empty;

        var routeParameter = ResolveIdParameter(action);
        if (routeParameter is null)
            return false;

        var parameterSubject = RemoveIdSuffix(routeParameter.ParameterName);
        if (string.IsNullOrWhiteSpace(parameterSubject))
            return false;

        var actionName = RemoveActionPrefix(action.ActionName);
        if (!actionName.StartsWith(parameterSubject, StringComparison.Ordinal))
            return false;

        var childResource = actionName[parameterSubject.Length..];
        if (string.IsNullOrWhiteSpace(childResource))
            return false;

        var parentSegment = ToResourceSegment(parameterSubject);
        var parameterTemplate = BuildRouteParameterTemplate(routeParameter);
        var childSegment = ToResourceSegment(childResource);
        template = $"{parentSegment}/{parameterTemplate}/{childSegment}";
        return true;
    }

    private static bool TryBuildCollectionTemplate(ActionModel action, out string template)
    {
        template = string.Empty;

        if (action.ActionName.EndsWith("s", StringComparison.Ordinal) && !HasKnownActionPrefix(action.ActionName))
        {
            template = ToResourceSegment(action.ActionName);
            return true;
        }

        var prefix = CollectionPrefixes.FirstOrDefault(prefix =>
            action.ActionName.StartsWith(prefix, StringComparison.Ordinal)
            && action.ActionName.Length > prefix.Length);

        if (prefix is null)
            return false;

        var resourceName = action.ActionName[prefix.Length..];
        template = ToResourceSegment(resourceName);

        if (prefix is "Delete" or "Patch")
            template = $"{template}/{BuildRouteParameterTemplate(action)}";

        return true;
    }

    private static bool HasKnownActionPrefix(string actionName)
        => CollectionPrefixes.Any(prefix => actionName.StartsWith(prefix, StringComparison.Ordinal));

    private static string RemoveActionPrefix(string actionName)
    {
        foreach (var prefix in new[] { "Get", "Create", "Update", "Delete" })
        {
            if (actionName.StartsWith(prefix, StringComparison.Ordinal) && actionName.Length > prefix.Length)
                return actionName[prefix.Length..];
        }

        return actionName;
    }

    private static string RemoveIdSuffix(string parameterName)
        => parameterName.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
            ? parameterName[..^2]
            : parameterName;

    private static string BuildRouteParameterTemplate(ActionModel action)
    {
        var parameter = ResolveIdParameter(action);
        return BuildRouteParameterTemplate(parameter);
    }

    private static string BuildRouteParameterTemplate(ParameterModel? parameter)
    {
        var parameterName = parameter?.ParameterName ?? "id";
        var constraint = ResolveRouteConstraint(parameter?.ParameterInfo.ParameterType);
        return string.IsNullOrWhiteSpace(constraint)
            ? $"{{{parameterName}}}"
            : $"{{{parameterName}:{constraint}}}";
    }

    private static ParameterModel? ResolveIdParameter(ActionModel action)
    {
        return action.Parameters
            .FirstOrDefault(parameter => parameter.ParameterName.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                || parameter.ParameterName.Equals("id", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveRouteConstraint(Type? parameterType)
    {
        var type = Nullable.GetUnderlyingType(parameterType ?? typeof(int)) ?? parameterType;
        return type == typeof(int)
            ? "int"
            : type == typeof(long)
                ? "long"
                : null;
    }

    private static string ToResourceSegment(string value)
    {
        var segment = ToKebabCase(value);
        return segment.EndsWith("s", StringComparison.Ordinal) || segment == "tree"
            ? segment
            : $"{segment}s";
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = new List<char>(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current) && i > 0)
                chars.Add('-');

            chars.Add(char.ToLowerInvariant(current));
        }

        return new string(chars.ToArray());
    }
}
