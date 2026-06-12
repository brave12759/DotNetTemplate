using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.Enums;
using Template.Common.Extensions;

namespace Template.Test.Tests;

[TestClass]
public class EnumExtensionsTests
{
    // ── GetDescription ──────────────────────────────────────────────

    [TestMethod]
    public void GetDescription_Returns_DescriptionAttribute_Text()
    {
        Assert.AreEqual("成功", MessageEnum.Success.GetDescription());
        Assert.AreEqual("未授權，請重新登入", MessageEnum.Unauthorized.GetDescription());
        Assert.AreEqual("請求參數錯誤", MessageEnum.BadRequest.GetDescription());
        Assert.AreEqual("請求過於頻繁", MessageEnum.TooManyRequests.GetDescription());
    }

    [TestMethod]
    public void GetDescription_NoAttribute_Falls_Back_To_Name()
    {
        // 使用測試專用 Enum（無 Description）
        Assert.AreEqual(nameof(PlainEnum.ItemA), PlainEnum.ItemA.GetDescription());
    }

    // ── ToInt ───────────────────────────────────────────────────────

    [TestMethod]
    public void ToInt_Returns_Correct_Value()
    {
        Assert.AreEqual(200, MessageEnum.Success.ToInt());
        Assert.AreEqual(400, MessageEnum.BadRequest.ToInt());
        Assert.AreEqual(401, MessageEnum.Unauthorized.ToInt());
        Assert.AreEqual(404, MessageEnum.NotFound.ToInt());
        Assert.AreEqual(429, MessageEnum.TooManyRequests.ToInt());
        Assert.AreEqual(504, MessageEnum.GatewayTimeout.ToInt());
    }

    // ── ToName ──────────────────────────────────────────────────────

    [TestMethod]
    public void ToName_Returns_Enum_Name_String()
    {
        Assert.AreEqual("Success", MessageEnum.Success.ToName());
        Assert.AreEqual("BadRequest", MessageEnum.BadRequest.ToName());
        Assert.AreEqual("TooManyRequests", MessageEnum.TooManyRequests.ToName());
    }

    // ── int.ToEnum<T> ────────────────────────────────────────────────

    [TestMethod]
    public void ToEnum_FromInt_ValidValue_Returns_Enum()
    {
        var result = 200.ToEnum<MessageEnum>();
        Assert.IsNotNull(result);
        Assert.AreEqual(MessageEnum.Success, result);
    }

    [TestMethod]
    public void ToEnum_FromInt_InvalidValue_Returns_Null()
    {
        var result = 999.ToEnum<MessageEnum>();
        Assert.IsNull(result);
    }

    // ── string.ToEnum<T> ─────────────────────────────────────────────

    [TestMethod]
    public void ToEnum_FromString_CaseInsensitive()
    {
        Assert.AreEqual(MessageEnum.Success, "success".ToEnum<MessageEnum>());
        Assert.AreEqual(MessageEnum.Success, "SUCCESS".ToEnum<MessageEnum>());
        Assert.AreEqual(MessageEnum.BadRequest, "badrequest".ToEnum<MessageEnum>());
    }

    [TestMethod]
    public void ToEnum_FromString_InvalidValue_Returns_Null()
    {
        var result = "NoSuchValue".ToEnum<MessageEnum>();
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValues_Should_ReturnAllEnumValues()
    {
        var values = EnumExtensions.GetValues<MessageEnum>();

        Assert.IsTrue(values.Contains(MessageEnum.Success));
        Assert.IsTrue(values.Contains(MessageEnum.BadRequest));
    }

    [TestMethod]
    public void GetDescriptionMap_Should_ContainDescriptionText()
    {
        var map = EnumExtensions.GetDescriptionMap<MessageEnum>();

        Assert.IsTrue(map.ContainsKey((int)MessageEnum.Success));
        Assert.AreEqual("成功", map[(int)MessageEnum.Success]);
    }
}

/// <summary>無 [Description] 標記的測試專用 Enum。</summary>
internal enum PlainEnum { ItemA, ItemB }
