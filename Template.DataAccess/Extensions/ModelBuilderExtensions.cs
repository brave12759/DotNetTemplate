using Microsoft.EntityFrameworkCore;
using Template.DataAccess.Converters;

namespace Template.DataAccess.Extensions;

/// <summary>
/// ModelBuilder 擴充方法。
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// 全域套用 UTC DateTime 值轉換器至所有實體的 DateTime / DateTime? 屬性。
    /// </summary>
    public static ModelBuilder ApplyUtcDateTimeConverter(this ModelBuilder modelBuilder)
    {
        var converter = new UtcDateTimeConverter();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(converter);
                }
            }
        }

        return modelBuilder;
    }
}
