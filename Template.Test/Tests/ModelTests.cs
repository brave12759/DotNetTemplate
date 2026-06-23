using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.Models;

namespace Template.Test.Tests;

[TestClass]
public class ResponseMessageTests
{
    [TestMethod]
    public void Success_Sets_Status200_Message_And_Content()
    {
        var result = ResponseMessage<string>.Success("hello");
        Assert.AreEqual(200, result.Status);
        Assert.AreEqual("成功", result.Message);
        Assert.AreEqual("hello", result.Details);
    }

    [TestMethod]
    public void Success_CustomMessage_IsPreserved()
    {
        var result = ResponseMessage<int>.Success(42, "自訂成功訊息");
        Assert.AreEqual(200, result.Status);
        Assert.AreEqual("自訂成功訊息", result.Message);
        Assert.AreEqual(42, result.Details);
    }

    [TestMethod]
    public void Success_NullContent_IsAllowed()
    {
        var result = ResponseMessage<string?>.Success(null);
        Assert.AreEqual(200, result.Status);
        Assert.IsNull(result.Details);
    }

    [TestMethod]
    public void Fail_Sets_Status_Message_And_NullContent()
    {
        var result = ResponseMessage<string>.Fail(400, "參數錯誤");
        Assert.AreEqual(400, result.Status);
        Assert.AreEqual("參數錯誤", result.Message);
        Assert.IsNull(result.Details);
    }

    [TestMethod]
    public void Fail_401_Unauthorized()
    {
        var result = ResponseMessage<object>.Fail(401, "未授權");
        Assert.AreEqual(401, result.Status);
        Assert.AreEqual("未授權", result.Message);
        Assert.IsNull(result.Details);
    }
}

[TestClass]
public class LoginResultTests
{
    [TestMethod]
    public void Ok_Sets_Success_True_And_Token()
    {
        var result = LoginResult.Ok("jwt-token-value");
        Assert.IsTrue(result.Success);
        Assert.AreEqual("jwt-token-value", result.Token);
        Assert.AreEqual(string.Empty, result.ErrorMessage);
    }

    [TestMethod]
    public void Fail_Sets_Success_False_And_ErrorMessage()
    {
        var result = LoginResult.Fail("帳號或密碼錯誤");
        Assert.IsFalse(result.Success);
        Assert.AreEqual("帳號或密碼錯誤", result.ErrorMessage);
        Assert.AreEqual(string.Empty, result.Token);
    }
}
