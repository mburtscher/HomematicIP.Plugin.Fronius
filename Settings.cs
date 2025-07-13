using System.Text.Json;

internal class Settings
{
    public const string Filename = "/data/config.json";

    public string? SolarApiHost { get; set; }

    private void Save()
    {
        File.WriteAllText(Filename, JsonSerializer.Serialize(this));
    }

    public static Settings FromFile()
    {
        if (File.Exists(Filename))
        {
            return JsonSerializer.Deserialize<Settings>(Filename) ?? new Settings();
        }

        return new Settings();
    }
}
