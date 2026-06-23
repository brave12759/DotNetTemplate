using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;
using Template.Common.Enums;
using Template.Common.Extensions;
using Template.Common.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Template.WebApi.Filters;

/// <summary>
/// 全域回傳包裝 Filter：將所有 Action 回傳值統一包成 ResponseMessage&lt;T&gt;。
/// 若 Action 或 Controller 標記 [SkipResponseWrap] 則跳過。
/// </summary>
public class ResponseWrapperFilter : IResultFilter, IOrderedFilter
{
    /// <summary>
    /// Filter 執行順序（越大越晚）。
    /// Result 包裝放在較後段，確保先取得最終狀態碼與結果內容。
    /// </summary>
    public int Order => int.MaxValue;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        // 已標記跳過
        if (context.ActionDescriptor.EndpointMetadata
                .Any(m => m is SkipResponseWrapAttribute))
            return;

        switch (context.Result)
        {
            // 已是 ResponseMessage<T>，不再包裝
            case ObjectResult { Value: not null } obj
                when obj.Value.GetType().IsGenericType &&
                     obj.Value.GetType().GetGenericTypeDefinition() == typeof(ResponseMessage<>):
                return;

            // 正常物件回傳
            case ObjectResult obj:
            {
                var statusCode = obj.StatusCode ?? context.HttpContext.Response.StatusCode;
                var wrapped = WrapValue(statusCode, obj.Value);
                context.Result = new ObjectResult(wrapped) { StatusCode = statusCode };
                break;
            }

            // 空回傳 (204 No Content / void)
            case EmptyResult:
            {
                var wrapped = ResponseMessage<object>.Success(null);
                context.Result = new ObjectResult(wrapped) { StatusCode = 200 };
                break;
            }

            case StatusCodeResult statusCodeResult:
            {
                var statusCode = statusCodeResult.StatusCode;
                var wrapped = WrapValue(statusCode, null);
                context.Result = new ObjectResult(wrapped) { StatusCode = statusCode };
                break;
            }
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }

    /// <summary>
    /// 將 Controller 回傳值包裝成統一 API 回應格式。
    /// </summary>
    private static object WrapValue(int statusCode, object? value)
    {
        var message = ResponseMessageMetadata.GetMessage(statusCode);
        var responseType = typeof(ResponseMessage<>).MakeGenericType(value?.GetType() ?? typeof(object));
        var factory = ResponseMessageMetadata.IsSuccess(statusCode)
            ? responseType.GetMethod(nameof(ResponseMessage<object>.Success))!
            : responseType.GetMethod(nameof(ResponseMessage<object>.Fail))!;

        return ResponseMessageMetadata.IsSuccess(statusCode)
            ? factory.Invoke(null, [value, message])!
            : factory.Invoke(null, [statusCode, message, value])!;
    }
}

/// <summary>
/// 將 Swagger 回應 schema 包裝成全域回傳格式。
/// </summary>
public sealed class ResponseMessageOperationFilter : IOperationFilter
{
    /// <summary>
    /// 套用 ResponseMessage 格式至所有 JSON 回應。
    /// </summary>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses ??= new OpenApiResponses();
        AddDefaultResponse(operation.Responses);

        foreach (var response in operation.Responses)
        {
            EnsureJsonContent(response.Value);

            var responseContent = response.Value.Content;
            if (responseContent is null)
            {
                continue;
            }

            foreach (var content in responseContent)
            {
                var schema = content.Value.Schema ?? CreateNullObjectSchema();
                if (!IsJsonContent(content.Key) || IsWrappedSchema(schema))
                {
                    continue;
                }

                schema = ResolveDetailsSchema(response.Key, schema, context);
                content.Value.Schema = CreateResponseMessageSchema(response.Key, schema);
            }
        }
    }

    private static void AddDefaultResponse(OpenApiResponses responses)
    {
        responses.TryAdd("default", new OpenApiResponse
        {
            Description = "其他全域回應",
            Content = new Dictionary<string, OpenApiMediaType>()
        });
    }

    private static OpenApiSchema CreateResponseMessageSchema(string statusCode, IOpenApiSchema detailsSchema)
    {
        var status = int.TryParse(statusCode, out var parsedStatus)
            ? parsedStatus
            : StatusCodes.Status400BadRequest;

        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Required = new HashSet<string>
            {
                "Status",
                "Message",
                "Details"
            },
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["Status"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Format = "int32",
                    Example = JsonValue.Create(status)
                },
                ["Message"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Example = JsonValue.Create(ResponseMessageMetadata.GetMessage(status))
                },
                ["Details"] = detailsSchema
            }
        };
    }

    private static void EnsureJsonContent(IOpenApiResponse response)
    {
        var responseContent = response.Content;
        if (responseContent is null && response is OpenApiResponse concreteResponse)
        {
            concreteResponse.Content = new Dictionary<string, OpenApiMediaType>();
            responseContent = concreteResponse.Content;
        }

        if (responseContent is null || responseContent.Count > 0)
        {
            return;
        }

        responseContent["application/json"] = new OpenApiMediaType
        {
            Schema = CreateNullObjectSchema()
        };
    }

    private static bool IsJsonContent(string contentType)
    {
        return contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWrappedSchema(IOpenApiSchema schema)
    {
        var properties = schema.Properties;
        return properties is not null
            && properties.ContainsKey("Status")
            && properties.ContainsKey("Message")
            && properties.ContainsKey("Details");
    }

    private static IOpenApiSchema ResolveDetailsSchema(
        string statusCode,
        IOpenApiSchema schema,
        OperationFilterContext? context)
    {
        if (!IsEmptySchema(schema) || context is null || !int.TryParse(statusCode, out var parsedStatus))
        {
            return schema;
        }

        var responseType = context.ApiDescription.SupportedResponseTypes
            .FirstOrDefault(response => response.StatusCode == parsedStatus)
            ?.Type;

        return responseType is null || responseType == typeof(void)
            ? schema
            : context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository);
    }

    private static bool IsEmptySchema(IOpenApiSchema schema)
    {
        return schema is OpenApiSchema openApiSchema
            && openApiSchema.Type is null
            && (openApiSchema.Properties?.Count ?? 0) == 0
            && openApiSchema.Items is null
            && (openApiSchema.AllOf?.Count ?? 0) == 0
            && (openApiSchema.OneOf?.Count ?? 0) == 0
            && (openApiSchema.AnyOf?.Count ?? 0) == 0;
    }

    private static OpenApiSchema CreateNullObjectSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object | JsonSchemaType.Null
        };
    }
}

internal static class ResponseMessageMetadata
{
    public static bool IsSuccess(int statusCode)
    {
        return statusCode is >= StatusCodes.Status200OK and < StatusCodes.Status400BadRequest;
    }

    public static string GetMessage(int statusCode)
    {
        return statusCode.ToEnum<MessageEnum>()?.GetDescription()
            ?? ReasonPhrases.GetReasonPhrase(statusCode);
    }
}
