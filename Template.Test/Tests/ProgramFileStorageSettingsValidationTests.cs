using System.Reflection;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.Settings;
using Template.WebApi.Filters;

namespace Template.Test.Tests;

[TestClass]
public class ProgramFileStorageSettingsValidationTests
{
    [TestMethod]
    public void ValidateFileStorageSettings_Disabled_Should_SkipAllValidation()
    {
        var settings = new FileStorageSettings
        {
            Enabled = false,
            Provider = string.Empty,
            EnableSingleUpload = false,
            EnableChunkUpload = false,
            EnableAdminScope = false,
            EnablePersonalScope = false,
            MaxFileSizeMb = -1,
            MaxSingleUploadSizeMb = -1,
            MaxChunkSizeMb = -1,
            MaxChunkCountPerFile = -1,
            ChunkSessionExpireMinutes = -1,
            DownloadUrlExpireSeconds = -1
        };

        InvokeValidateFileStorageSettings(settings);
    }

    [TestMethod]
    public void ValidateFileStorageSettings_ValidEnabledSettings_Should_NotThrow()
    {
        var settings = new FileStorageSettings
        {
            Enabled = true,
            Provider = "Custom",
            EnableSingleUpload = true,
            EnableChunkUpload = true,
            EnableAdminScope = true,
            EnablePersonalScope = true,
            MaxFileSizeMb = 100,
            MaxSingleUploadSizeMb = 50,
            MaxChunkSizeMb = 10,
            MaxChunkCountPerFile = 10,
            ChunkSessionExpireMinutes = 30,
            DownloadUrlExpireSeconds = 120
        };

        InvokeValidateFileStorageSettings(settings);
    }

    [TestMethod]
    public void ValidateFileStorageSettings_EmptyProvider_Should_Throw()
    {
        var settings = CreateEnabledSettings();
        settings.Provider = "  ";

        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeValidateFileStorageSettings(settings));
        StringAssert.Contains(ex.Message, "Provider is required");
    }

    [TestMethod]
    public void ValidateFileStorageSettings_AllUploadModesDisabled_Should_Throw()
    {
        var settings = CreateEnabledSettings();
        settings.EnableSingleUpload = false;
        settings.EnableChunkUpload = false;

        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeValidateFileStorageSettings(settings));
        StringAssert.Contains(ex.Message, "At least one upload mode");
    }

    [TestMethod]
    public void ValidateFileStorageSettings_AllScopesDisabled_Should_Throw()
    {
        var settings = CreateEnabledSettings();
        settings.EnableAdminScope = false;
        settings.EnablePersonalScope = false;

        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeValidateFileStorageSettings(settings));
        StringAssert.Contains(ex.Message, "At least one file scope");
    }

    [TestMethod]
    public void ValidateFileStorageSettings_SingleUploadExceedsMaxFile_Should_Throw()
    {
        var settings = CreateEnabledSettings();
        settings.MaxFileSizeMb = 100;
        settings.MaxSingleUploadSizeMb = 101;

        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeValidateFileStorageSettings(settings));
        StringAssert.Contains(ex.Message, "cannot exceed MaxFileSizeMb");
    }

    [TestMethod]
    public void ValidateFileStorageSettings_ChunkCapacityInsufficient_Should_Throw()
    {
        var settings = CreateEnabledSettings();
        settings.MaxFileSizeMb = 101;
        settings.MaxChunkSizeMb = 10;
        settings.MaxChunkCountPerFile = 10;

        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeValidateFileStorageSettings(settings));
        StringAssert.Contains(ex.Message, "Chunk capacity");
    }

    [TestMethod]
    public void ValidateFileStorageSettings_NonPositiveTimeout_Should_Throw()
    {
        var settings = CreateEnabledSettings();
        settings.DownloadUrlExpireSeconds = 0;

        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeValidateFileStorageSettings(settings));
        StringAssert.Contains(ex.Message, "DownloadUrlExpireSeconds");
    }

    private static FileStorageSettings CreateEnabledSettings()
    {
        return new FileStorageSettings
        {
            Enabled = true,
            Provider = "Custom",
            EnableSingleUpload = true,
            EnableChunkUpload = true,
            EnableAdminScope = true,
            EnablePersonalScope = true,
            MaxFileSizeMb = 100,
            MaxSingleUploadSizeMb = 80,
            MaxChunkSizeMb = 10,
            MaxChunkCountPerFile = 10,
            ChunkSessionExpireMinutes = 120,
            DownloadUrlExpireSeconds = 300
        };
    }

    private static void InvokeValidateFileStorageSettings(FileStorageSettings settings)
    {
        var programType = typeof(ResponseWrapperFilter).Assembly.GetType("Program")
            ?? throw new AssertFailedException("Program type was not found in Template.WebApi assembly.");

        var method = programType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
                m.Name.Contains("ValidateFileStorageSettings", StringComparison.Ordinal) &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(FileStorageSettings))
            ?? throw new AssertFailedException("ValidateFileStorageSettings method was not found.");

        try
        {
            method.Invoke(null, [settings]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
