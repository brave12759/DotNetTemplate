using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.Models;
using Template.WebApi.Converters;
using Template.WebApi.Filters;

namespace Template.Test.Tests;

[TestClass]
public class ResponseWrapperFilterTests
{
    [TestMethod]
    public void OnResultExecuting_ObjectResult_Should_WrapSuccessResponse()
    {
        var filter = new ResponseWrapperFilter();
        var context = CreateContext(new ObjectResult("hello") { StatusCode = 200 });

        filter.OnResultExecuting(context);

        var result = (ObjectResult)context.Result;
        var response = (ResponseMessage<string>)result.Value!;
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("hello", response.Content);
    }

    [TestMethod]
    public void OnResultExecuting_ErrorObjectResult_Should_WrapFailResponse()
    {
        var filter = new ResponseWrapperFilter();
        var context = CreateContext(new ObjectResult("bad request") { StatusCode = 400 });

        filter.OnResultExecuting(context);

        var result = (ObjectResult)context.Result;
        var response = (ResponseMessage<string>)result.Value!;
        Assert.AreEqual(400, result.StatusCode);
        Assert.AreEqual(400, response.Status);
        Assert.AreEqual("bad request", response.Message);
        Assert.IsNull(response.Content);
    }

    [TestMethod]
    public void OnResultExecuting_ResponseMessage_Should_NotWrapAgain()
    {
        var filter = new ResponseWrapperFilter();
        var original = ResponseMessage<string>.Success("ok");
        var context = CreateContext(new ObjectResult(original) { StatusCode = 200 });

        filter.OnResultExecuting(context);

        var result = (ObjectResult)context.Result;
        Assert.AreSame(original, result.Value);
    }

    [TestMethod]
    public void OnResultExecuting_EmptyResult_Should_WrapNullSuccess()
    {
        var filter = new ResponseWrapperFilter();
        var context = CreateContext(new EmptyResult());

        filter.OnResultExecuting(context);

        var result = (ObjectResult)context.Result;
        var response = (ResponseMessage<object>)result.Value!;
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual(200, response.Status);
        Assert.IsNull(response.Content);
    }

    [TestMethod]
    public void OnResultExecuting_SkipResponseWrapMetadata_Should_KeepOriginalResult()
    {
        var filter = new ResponseWrapperFilter();
        var original = new ObjectResult("hello") { StatusCode = 200 };
        var context = CreateContext(original, new SkipResponseWrapAttribute());

        filter.OnResultExecuting(context);

        Assert.AreSame(original, context.Result);
    }

    private static ResultExecutingContext CreateContext(IActionResult result, params object[] endpointMetadata)
    {
        var actionDescriptor = new ActionDescriptor
        {
            EndpointMetadata = endpointMetadata.ToList()
        };

        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            actionDescriptor);

        return new ResultExecutingContext(
            actionContext,
            [],
            result,
            controller: new object());
    }
}

[TestClass]
public class DateTimeJsonConverterTests
{
    private static readonly TimeZoneInfo PlusEightTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "UTC+08-json",
        TimeSpan.FromHours(8),
        "UTC+08-json",
        "UTC+08-json");

    [TestMethod]
    public void DateTimeJsonConverter_WriteUtc_Should_OutputTargetTimeZoneOffset()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeJsonConverter(PlusEightTimeZone));
        var value = new DateTime(2026, 5, 8, 5, 20, 30, 456, DateTimeKind.Utc);

        var json = JsonSerializer.Serialize(value, options);

        Assert.AreEqual("2026-05-08T13:20:30.456+08:00", JsonSerializer.Deserialize<string>(json));
    }

    [TestMethod]
    public void DateTimeJsonConverter_Read_Should_ParseString()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeJsonConverter(PlusEightTimeZone));

        var value = JsonSerializer.Deserialize<DateTime>("\"2026-05-08T13:20:30\"", options);

        Assert.AreEqual(new DateTime(2026, 5, 8, 13, 20, 30), value);
    }

    [TestMethod]
    public void DateTimeOffsetJsonConverter_Write_Should_OutputTargetTimeZoneOffset()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeOffsetJsonConverter(PlusEightTimeZone));
        var value = new DateTimeOffset(2026, 5, 8, 5, 20, 30, 456, TimeSpan.Zero);

        var json = JsonSerializer.Serialize(value, options);

        Assert.AreEqual("2026-05-08T13:20:30.456+08:00", JsonSerializer.Deserialize<string>(json));
    }

    [TestMethod]
    public void DateTimeOffsetJsonConverter_Read_Should_ParseString()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeOffsetJsonConverter(PlusEightTimeZone));

        var value = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-05-08T13:20:30+08:00\"", options);

        Assert.AreEqual(new DateTimeOffset(2026, 5, 8, 13, 20, 30, TimeSpan.FromHours(8)), value);
    }
}
