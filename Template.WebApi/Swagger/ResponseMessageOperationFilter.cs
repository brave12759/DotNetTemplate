using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Template.Common.Enums;
using Template.Common.Extensions;

namespace Template.WebApi.Swagger;

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
                    Example = JsonValue.Create(GetMessage(status))
                },
                ["Details"] = detailsSchema
            }
        };
    }

    private static string GetMessage(int status)
    {
        return status.ToEnum<MessageEnum>()?.GetDescription()
            ?? ReasonPhrases.GetReasonPhrase(status);
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

    private static OpenApiSchema CreateNullObjectSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object | JsonSchemaType.Null
        };
    }
}
