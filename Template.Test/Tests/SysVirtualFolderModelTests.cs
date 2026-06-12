using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class SysVirtualFolderModelTests
{
    [TestMethod]
    public void SysVirtualFolder_Should_AssignPropertiesAndNavigation()
    {
        var now = DateTime.UtcNow;
        var root = new Sys_VirtualFolder
        {
            Id = 1,
            Scope = 2,
            OwnerUserId = "admin",
            FolderName = "Root",
            FolderPath = "/Root",
            ParentFolderId = null,
            SortOrder = 1,
            IsEnable = true,
            CreatedTime = now,
            CreatedId = "seed",
            UpdatedTime = now,
            UpdatedId = "seed"
        };

        var child = new Sys_VirtualFolder
        {
            Id = 2,
            Scope = 2,
            OwnerUserId = "admin",
            FolderName = "Child",
            FolderPath = "/Root/Child",
            ParentFolderId = root.Id,
            SortOrder = 2,
            IsEnable = true,
            CreatedTime = now,
            CreatedId = "seed",
            UpdatedTime = now,
            UpdatedId = "seed",
            ParentFolder = root
        };

        root.InverseParentFolder.Add(child);

        Assert.AreEqual("/Root", root.FolderPath);
        Assert.AreEqual(root, child.ParentFolder);
        Assert.AreEqual(1, root.InverseParentFolder.Count);
    }

    [TestMethod]
    public void SysVirtualFolder_Should_HaveRequiredAttributesOnCriticalFields()
    {
        var ownerProp = typeof(Sys_VirtualFolder).GetProperty(nameof(Sys_VirtualFolder.OwnerUserId));
        var folderNameProp = typeof(Sys_VirtualFolder).GetProperty(nameof(Sys_VirtualFolder.FolderName));
        var folderPathProp = typeof(Sys_VirtualFolder).GetProperty(nameof(Sys_VirtualFolder.FolderPath));

        Assert.IsNotNull(ownerProp);
        Assert.IsNotNull(folderNameProp);
        Assert.IsNotNull(folderPathProp);

        Assert.IsNotNull(ownerProp!.GetCustomAttributes(typeof(RequiredAttribute), inherit: false).SingleOrDefault());
        Assert.IsNotNull(folderNameProp!.GetCustomAttributes(typeof(RequiredAttribute), inherit: false).SingleOrDefault());
        Assert.IsNotNull(folderPathProp!.GetCustomAttributes(typeof(RequiredAttribute), inherit: false).SingleOrDefault());
    }
}
