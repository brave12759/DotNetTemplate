using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.Settings;
using Template.WebApi.Authentication;
using Template.WebApi.Filters;

namespace Template.Test.Tests;

[TestClass]
public class WebApiSettingsAndAttributeTests
{
    [TestMethod]
    public void RequirePermissionAttribute_Should_StorePermissionKeysAndType()
    {
        var attr = new RequirePermissionAttribute("A:Read", "B:Write");

        Assert.AreEqual(typeof(RequirePermissionFilter), attr.ImplementationType);
        CollectionAssert.AreEqual(new[] { "A:Read", "B:Write" }, attr.PermissionKeys);
        Assert.IsNotNull(attr.Arguments);
        Assert.AreEqual(1, attr.Arguments.Length);
        var argKeys = attr.Arguments[0] as string[];
        Assert.IsNotNull(argKeys);
        CollectionAssert.AreEqual(attr.PermissionKeys, argKeys);
    }

    [TestMethod]
    public void ApiSettings_Defaults_Should_BeExpected()
    {
        var settings = new ApiSettings();
        Assert.AreEqual(ApiSettings.SectionName, "ApiSettings");
        Assert.AreEqual(string.Empty, settings.Name);
    }

    [TestMethod]
    public void DatabaseSettings_Defaults_Should_BeExpected()
    {
        var settings = new DatabaseSettings();
        Assert.AreEqual(DatabaseSettings.SectionName, "DatabaseSettings");
        Assert.AreEqual(string.Empty, settings.ProjectConnectionString);
        Assert.AreEqual(string.Empty, settings.LogConnectionString);
    }

    [TestMethod]
    public void TimeZoneSettings_Defaults_Should_BeExpected()
    {
        var settings = new TimeZoneSettings();
        Assert.AreEqual(TimeZoneSettings.SectionName, "TimeZoneSettings");
        Assert.AreEqual("Asia/Taipei", settings.TimeZoneId);
    }

    [TestMethod]
    public void CorsSettings_Defaults_Should_BeExpected()
    {
        var settings = new CorsSettings();
        Assert.AreEqual(CorsSettings.SectionName, "CorsSettings");
        Assert.IsFalse(settings.AllowAnyOrigin);
        Assert.IsFalse(settings.AllowCredentials);
        Assert.IsNotNull(settings.AllowedOrigins);
        Assert.AreEqual(0, settings.AllowedOrigins.Length);
    }

    [TestMethod]
    public void LogSettings_Defaults_Should_BeExpected()
    {
        var settings = new LogSettings();
        Assert.AreEqual(LogSettings.SectionName, "LogSettings");
        Assert.AreEqual("Logs", settings.LogDirectory);
        Assert.AreEqual(50, settings.FileSizeLimitMb);
        Assert.AreEqual(100, settings.RetainedFileCountLimit);
        Assert.AreEqual("Warning", settings.MinimumLevel);
    }

    [TestMethod]
    public void DevBypassUserSettings_Defaults_Should_BeExpected()
    {
        var settings = new DevBypassUserSettings();
        Assert.AreEqual(DevBypassUserSettings.SectionName, "DevBypassUser");
        Assert.AreEqual("dev", settings.UserId);
        Assert.AreEqual("dev@localhost", settings.Email);
        Assert.AreEqual(string.Empty, settings.MobilePhone);
        Assert.AreEqual("0", settings.DeptId);
    }
}
