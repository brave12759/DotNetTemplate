using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.ProjectDbContext;

public partial class ProjectDbContext
{
    public virtual DbSet<Sys_BackgroundJob> Sys_BackgroundJobs { get; set; } = null!;
}
