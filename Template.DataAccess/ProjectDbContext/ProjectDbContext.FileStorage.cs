using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.ProjectDbContext;

public partial class ProjectDbContext
{
    public virtual DbSet<Sys_Attachment> Sys_Attachments { get; set; }

    public virtual DbSet<Sys_VirtualFolder> Sys_VirtualFolders { get; set; }
}
