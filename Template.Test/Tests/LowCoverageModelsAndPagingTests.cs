using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.LogService.Models;
using Template.Common.Enums;
using Template.Common.FileStorage;
using Template.Common.Models;

namespace Template.Test.Tests;

[TestClass]
public class PageListOutputAndPagingTests
{
    [TestMethod]
    public void PageListOutput_TotalPagesAndFlags_Should_Work()
    {
        var paged = new PageListOutput<int> { TotalCount = 21, Page = 2, PageSize = 10, IsPaged = true, Items = [11, 12] };
        var nonPaged = new PageListOutput<int> { TotalCount = 3, IsPaged = false, Items = [1, 2, 3] };
        var empty = new PageListOutput<int> { TotalCount = 0, IsPaged = true, Page = 1, PageSize = 10 };

        Assert.AreEqual(3, paged.TotalPages);
        Assert.IsTrue(paged.HasPreviousPage);
        Assert.IsTrue(paged.HasNextPage);

        Assert.AreEqual(1, nonPaged.TotalPages);
        Assert.IsFalse(nonPaged.HasPreviousPage);
        Assert.IsFalse(nonPaged.HasNextPage);

        Assert.AreEqual(0, empty.TotalPages);
    }

    [TestMethod]
    public void ToPageListOutput_EnablePaging_Should_PageCorrectly()
    {
        var result = Enumerable.Range(1, 25).ToPageListOutput(page: 2, pageSize: 10, enablePaging: true);

        Assert.AreEqual(25, result.TotalCount);
        Assert.AreEqual(2, result.Page);
        Assert.AreEqual(10, result.PageSize);
        Assert.IsTrue(result.IsPaged);
        CollectionAssert.AreEqual(new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }, result.Items.ToArray());
    }

    [TestMethod]
    public async Task ToPageListOutputAsync_NoPaging_Should_ReturnAll()
    {
        var options = new DbContextOptionsBuilder<PagingTestDbContext>()
            .UseInMemoryDatabase($"paging-tests-no-page-{Guid.NewGuid():N}")
            .Options;

        await using var db = new PagingTestDbContext(options);
        db.Numbers.AddRange(Enumerable.Range(1, 5).Select(x => new NumberEntity { Value = x }));
        await db.SaveChangesAsync();

        var result = await db.Numbers
            .OrderBy(x => x.Value)
            .Select(x => x.Value)
            .ToPageListOutputAsync(enablePaging: false);

        Assert.AreEqual(5, result.TotalCount);
        Assert.AreEqual(1, result.Page);
        Assert.AreEqual(5, result.PageSize);
        Assert.IsFalse(result.IsPaged);
    }

    [TestMethod]
    public async Task ToPageListOutputAsync_EnablePaging_Should_Work()
    {
        var options = new DbContextOptionsBuilder<PagingTestDbContext>()
            .UseInMemoryDatabase($"paging-tests-{Guid.NewGuid():N}")
            .Options;

        await using var db = new PagingTestDbContext(options);
        db.Numbers.AddRange(Enumerable.Range(1, 30).Select(x => new NumberEntity { Value = x }));
        await db.SaveChangesAsync();

        var result = await db.Numbers
            .OrderBy(x => x.Value)
            .Select(x => x.Value)
            .ToPageListOutputAsync(page: 3, pageSize: 7, enablePaging: true);

        Assert.AreEqual(30, result.TotalCount);
        Assert.AreEqual(3, result.Page);
        Assert.AreEqual(7, result.PageSize);
        CollectionAssert.AreEqual(new[] { 15, 16, 17, 18, 19, 20, 21 }, result.Items.ToArray());
    }

    [TestMethod]
    public void ValidatePaging_InvalidValue_Should_Throw()
    {
        Assert.ThrowsException<ArgumentException>(() => PageListQueryableExtensions.ValidatePaging(0, 10));
        Assert.ThrowsException<ArgumentException>(() => PageListQueryableExtensions.ValidatePaging(1, 0));
        Assert.ThrowsException<ArgumentException>(() => PageListQueryableExtensions.ValidatePaging(1, 201));
    }

    private sealed class PagingTestDbContext(DbContextOptions<PagingTestDbContext> options) : DbContext(options)
    {
        public DbSet<NumberEntity> Numbers => Set<NumberEntity>();
    }

    private sealed class NumberEntity
    {
        public int Id { get; set; }
        public int Value { get; set; }
    }
}

[TestClass]
public class CommonModelCoverageTests
{
    [TestMethod]
    public void ResponseMessage_FactoryMethods_Should_ReturnExpected()
    {
        var success = ResponseMessage<string>.Success("ok", "done");
        var fail = ResponseMessage<string>.Fail(400, "bad");
        var from = ResponseMessage<string>.From(MessageEnum.NotFound, "x");

        Assert.AreEqual(200, success.Status);
        Assert.AreEqual("done", success.Message);
        Assert.AreEqual("ok", success.Content);

        Assert.AreEqual(400, fail.Status);
        Assert.AreEqual("bad", fail.Message);
        Assert.IsNull(fail.Content);

        Assert.AreEqual((int)MessageEnum.NotFound, from.Status);
        Assert.AreEqual("x", from.Content);
        Assert.IsFalse(string.IsNullOrWhiteSpace(from.Message));
    }

    [TestMethod]
    public void LogDtos_Should_AssignProperties()
    {
        var ssoCreate = new SsoLogCreateRequest { OperatorId = "op", ClientId = "c", EventName = "e", Result = "r", IpAddress = "ip", Message = "m", Metadata = new { A = 1 } };
        var ssoResult = new SsoLogQueryResult { TotalCount = 1, Page = 1, PageSize = 10, Items = [new SsoLogDto { Id = 1, OperatorId = "op", ClientId = "c", EventName = "e", Result = "r", IpAddress = "ip", Message = "m", MetadataJson = "{}" }] };
        var queueCreate = new QueueLogCreateRequest { OperatorId = "op", JobId = 1, WorkType = 2, WorkKey = "k", EventName = "e", Status = 1, RetryCount = 0, Message = "m", ErrorMessage = "" };
        var queueResult = new QueueLogQueryResult { TotalCount = 1, Page = 1, PageSize = 10, Items = [new QueueLogDto { Id = 1, OperatorId = "op", JobId = 1, WorkType = 2, WorkKey = "k", EventName = "e", Status = 1, RetryCount = 0, Message = "m", ErrorMessage = "", MetadataJson = "{}" }] };

        Assert.AreEqual("op", ssoCreate.OperatorId);
        Assert.AreEqual(1, ssoResult.Items.Count);
        Assert.AreEqual(1L, queueCreate.JobId);
        Assert.AreEqual(1, queueResult.Items.Count);
    }

    [TestMethod]
    public void FileStorageContracts_RemainingDtos_Should_AssignProperties()
    {
        var init = new ChunkUploadInitRequest { RequestUserId = "u", Scope = FileScope.Admin, FileName = "a.txt", TotalSize = 10, ChunkSize = 2, TotalChunks = 5, FolderPath = "/a", Tags = ["x"] };
        var part = new ChunkUploadPartRequest { UploadId = "up", ChunkIndex = 1, ChunkSize = 2, Content = new MemoryStream([1, 2]), Checksum = "sum" };
        var complete = new ChunkUploadCompleteRequest { UploadId = "up", ChunkIndexes = [1, 2] };
        var downloadReq = new FileDownloadRequest { RequestUserId = "u", Scope = FileScope.Admin, FileId = "f1" };
        var listReq = new FileListQueryRequest { RequestUserId = "u", Scope = FileScope.Admin, FolderPath = "/a", Keyword = "k", Page = 2, PageSize = 10 };
        var delReq = new FileDeleteRequest { RequestUserId = "u", Scope = FileScope.Admin, FileId = "f1" };
        var partResult = new ChunkUploadPartResult { UploadId = "up", ChunkIndex = 1, Success = true, UploadedChunks = 1 };
        var downloadResult = new FileDownloadResult { FileId = "f1", FileName = "a.txt", ContentType = "text/plain", ContentLength = 2, Content = new MemoryStream([1, 2]) };
        var folderDto = new VirtualFolderDto { FolderId = "1", FolderName = "A", Path = "/A", ParentPath = "/", ChildrenCount = 1, FileCount = 2 };
        var uploadMode = FileUploadMode.Chunk;

        Assert.AreEqual("u", init.RequestUserId);
        Assert.AreEqual("up", part.UploadId);
        Assert.AreEqual(2, complete.ChunkIndexes.Length);
        Assert.AreEqual("f1", downloadReq.FileId);
        Assert.AreEqual("k", listReq.Keyword);
        Assert.AreEqual("f1", delReq.FileId);
        Assert.IsTrue(partResult.Success);
        Assert.AreEqual(2, downloadResult.ContentLength);
        Assert.AreEqual("A", folderDto.FolderName);
        Assert.AreEqual(FileUploadMode.Chunk, uploadMode);
    }
}
