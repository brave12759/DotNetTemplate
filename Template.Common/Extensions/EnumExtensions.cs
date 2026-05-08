using System.ComponentModel;
using System.Reflection;

namespace Template.Common.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// 取得 [Description] 屬性的文字，若未標記則回傳 Enum 名稱
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        return value.GetType()
            .GetField(value.ToString())
            ?.GetCustomAttribute<DescriptionAttribute>()
            ?.Description ?? value.ToString();
    }

    /// <summary>
    /// 取得 Enum 對應的 int 值
    /// </summary>
    public static int ToInt(this Enum value) => Convert.ToInt32(value);

    /// <summary>
    /// 取得 Enum 名稱字串
    /// </summary>
    public static string ToName(this Enum value) => value.ToString();

    /// <summary>
    /// 將 int 轉換為指定的 Enum，失敗時回傳 null
    /// </summary>
    public static T? ToEnum<T>(this int value) where T : struct, Enum =>
        Enum.IsDefined(typeof(T), value) ? (T)(object)value : null;

    /// <summary>
    /// 將字串轉換為指定的 Enum（不分大小寫），失敗時回傳 null
    /// </summary>
    public static T? ToEnum<T>(this string value) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : null;

    /// <summary>
    /// 取得指定 Enum 型別所有值的清單
    /// </summary>
    public static IReadOnlyList<T> GetValues<T>() where T : struct, Enum =>
        Enum.GetValues<T>();

    /// <summary>
    /// 取得指定 Enum 型別所有值與 Description 的字典
    /// </summary>
    public static IReadOnlyDictionary<int, string> GetDescriptionMap<T>() where T : struct, Enum =>
        Enum.GetValues<T>()
            .ToDictionary(e => Convert.ToInt32(e), e => ((Enum)(object)e).GetDescription());
}
