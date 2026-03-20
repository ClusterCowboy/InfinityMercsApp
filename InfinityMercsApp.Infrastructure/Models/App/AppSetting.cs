using SQLite;

namespace InfinityMercsApp.Infrastructure.Models.App;

[Table("app_settings")]
public class AppSetting
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
