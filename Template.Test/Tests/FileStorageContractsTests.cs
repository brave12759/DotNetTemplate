using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.FileStorage;

namespace Template.Test.Tests;

[TestClass]
public class FileStorageContractsTests
{
    [TestMethod]
    public void SingleFileUploadRequest_Defaults_Should_BeExpected()
    {
        var request = new SingleFileUploadRequest();

        Assert.AreEqual(string.Empty, request.RequestUserId);
        Assert.AreEqual(FileScope.Personal, request.Scope);
        Assert.AreEqual("application/octet-stream", request.ContentType);
        Assert.AreEqual(0L, request.ContentLength);
        Assert.IsNotNull(request.Content);
        Assert.IsNotNull(request.Tags);
        Assert.AreEqual(0, request.Tags.Length);
    }

    [TestMethod]
    public void ChunkUploadSession_Defaults_Should_BeExpected()
    {
        var session = new ChunkUploadSession();

        Assert.AreEqual(string.Empty, session.UploadId);
        Assert.AreEqual(string.Empty, session.FileName);
        Assert.AreEqual("application/octet-stream", session.ContentType);
        Assert.AreEqual(0L, session.TotalSize);
        Assert.AreEqual(0, session.ChunkSize);
        Assert.AreEqual(0, session.TotalChunks);
        Assert.AreEqual(0, session.UploadedChunks);
    }

    [TestMethod]
    public void FileEntryDto_Defaults_Should_BeExpected()
    {
        var dto = new FileEntryDto();

        Assert.AreEqual(string.Empty, dto.FileId);
        Assert.AreEqual(string.Empty, dto.FileName);
        Assert.AreEqual(string.Empty, dto.Extension);
        Assert.AreEqual("application/octet-stream", dto.ContentType);
        Assert.AreEqual(0L, dto.SizeBytes);
        Assert.AreEqual(string.Empty, dto.OwnerUserId);
        Assert.AreEqual(string.Empty, dto.FolderPath);
        Assert.IsNotNull(dto.Tags);
        Assert.AreEqual(0, dto.Tags.Length);
    }

    [TestMethod]
    public void FileListResult_DefaultPaging_Should_BeExpected()
    {
        var result = new FileListResult();

        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(1, result.Page);
        Assert.AreEqual(0, result.PageSize);
        Assert.IsNotNull(result.Items);
        Assert.AreEqual(0, result.Items.Count);
    }

    [TestMethod]
    public void VirtualFolderRequests_Defaults_Should_BeExpected()
    {
        var create = new VirtualFolderCreateRequest();
        var update = new VirtualFolderUpdateRequest();
        var delete = new VirtualFolderDeleteRequest();
        var list = new VirtualFolderListRequest();

        Assert.AreEqual(FileScope.Personal, create.Scope);
        Assert.AreEqual(FileScope.Personal, update.Scope);
        Assert.AreEqual(FileScope.Personal, delete.Scope);
        Assert.AreEqual(FileScope.Personal, list.Scope);
        Assert.IsFalse(delete.Recursive);
    }
}
