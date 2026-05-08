using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.ProjectDbContext;

public partial class ProjectDbContext
{
    public virtual DbSet<Sys_MenuTree> Sys_MenuTrees { get; set; } = null!;
}
