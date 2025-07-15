using System.ComponentModel.DataAnnotations;
using System.Text.Json;

internal class Settings : IValidatableObject
{
#if DEBUG
    public const string Filename = "./config.json";
#else
    public const string Filename = "/data/config.json";
#endif

    [Url]
    [Required]
    public string? SolarApiBaseUrl { get; set; }

    public void Save()
    {
        File.WriteAllText(Filename, JsonSerializer.Serialize(this));
    }

    public Settings Clone()
    {
        return new Settings
        {
            SolarApiBaseUrl = SolarApiBaseUrl,
        };
    }

    public void Apply(Settings newValues)
    {
        SolarApiBaseUrl = newValues.SolarApiBaseUrl;
    }

    public static Settings FromFile()
    {
        if (File.Exists(Filename))
        {
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(Filename)) ?? new Settings();
        }

        return new Settings();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!SolarApiClient.ValidateBaseUrl(SolarApiBaseUrl))
        {
            yield return new ValidationResult($"API endpoint not available under {SolarApiBaseUrl}.", [nameof(SolarApiBaseUrl)]);
        }
    }
}
