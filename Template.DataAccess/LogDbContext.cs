using Microsoft.EntityFrameworkCore;
using Template.DataAccess.Extensions;

namespace Template.DataAccess.LogDbContext;

public partial class LogDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyUtcDateTimeConverter();
    }
}
