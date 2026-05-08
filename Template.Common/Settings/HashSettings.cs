namespace Template.Common.Settings;

public class HashSettings
{
    public const string SectionName = "HashSettings";

    /// <summary>
    /// PBKDF2 迭代次數
    /// </summary>
    public int Iterations { get; set; } = 100000;
}
