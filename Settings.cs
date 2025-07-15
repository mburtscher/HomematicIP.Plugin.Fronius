using HomematicIP.Domain;
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
    public string? SolarApiBaseUrl { get; set; }

    public void Save()
    {
        File.WriteAllText(Filename, JsonSerializer.Serialize(this));
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
        string apiUrl = $"{SolarApiBaseUrl}/solar_api/v1/GetPowerFlowRealtimeData.fcgi";

        using (HttpClient client = new HttpClient())
        {
            var success = false;

            try
            {
                var response = client.GetAsync(apiUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    success = true;
                }
            }
            catch { }

            if (!success)
            {
                yield return new ValidationResult($"Endpoint {apiUrl} not available.", [ nameof(SolarApiBaseUrl) ]);
            }
        }
    }
}
