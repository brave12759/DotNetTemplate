namespace Template.Common.Settings;

public class DatabaseSettings
{
    public const string SectionName = "DatabaseSettings";

    public string ProjectConnectionString { get; set; } = string.Empty;
    public string LogConnectionString { get; set; } = string.Empty;
}
