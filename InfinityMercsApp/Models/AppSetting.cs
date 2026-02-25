using SQLite;

namespace InfinityMercsApp.Models;

[Table("app_settings")]
public class AppSetting
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, Unique]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
