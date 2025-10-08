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
    public string? InverterIpAddress { get; set; }

    public void Save()
    {
        File.WriteAllText(Filename, JsonSerializer.Serialize(this));
    }

    public Settings Clone()
    {
        return new Settings
        {
            InverterIpAddress = InverterIpAddress,
        };
    }

    public void Apply(Settings newValues)
    {
        InverterIpAddress = newValues.InverterIpAddress;
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
        if (!SolarApiClient.ValidateBaseUrl(InverterIpAddress))
        {
            yield return new ValidationResult($"API endpoint not available under {InverterIpAddress}.", [nameof(InverterIpAddress)]);
        }
    }
}
