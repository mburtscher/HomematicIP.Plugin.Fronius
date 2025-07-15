
using HomematicIP;
using HomematicIP.Api;
using HomematicIP.Domain;
using HomematicIP.Domain.Features;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

var configuration = Settings.FromFile();
var plugin = new Plugin("xyz.burtscher.homematic.plugin.fronius");
var client = new SolarApiClient(configuration);

plugin.RegisterHandler<ConfigTemplateRequest>(HandleConfigTemplateRequest);
plugin.RegisterHandler<ConfigUpdateRequest>(HandleConfigUpdateRequest);
plugin.RegisterHandler<DiscoverRequest>(HandleDiscoverRequest);
plugin.RegisterHandler<InclusionEvent>(HandleInclusionEvent);
plugin.RegisterHandler<StatusRequest>(HandleStatusRequest);

#if DEBUG
plugin.Host = "192.168.1.122";
plugin.Token = "CBEB0C80B57A1E1EB43666DA38785BF3F539414CE5C93F15F661ABDFEEA82D46";
#endif

Task startTask = plugin.Start();
Task periodicTask = RunPeriodicTaskAsync(plugin, new CancellationToken());

await Task.WhenAll(startTask, periodicTask);

async Task RunPeriodicTaskAsync(Plugin plugin, CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        await Task.Delay(10000);

        await plugin.Send(new StatusResponse
        {
            Body = new StatusResponseBody
            {
                Success = true,
                Devices = client.GetDevices(),
            },
        });
    }
}

void HandleConfigTemplateRequest(Plugin plugin, ConfigTemplateRequest message)
{
    Thread.Sleep(1000);

    plugin.Send(new ConfigTemplateResponse
    {
        Id = message.Id,
        Body = new ConfigTemplateResponseBody
        {
            Groups = new Dictionary<string, GroupTemplate>
            {
                {
                    "api",
                    new GroupTemplate
                    {
                        FriendlyName = "Solar API",
                    }
                }
            },
            Properties = new Dictionary<string, PropertyTemplate>
            {
                {
                    "solarApiBaseUrl",
                    new PropertyTemplate
                    {
                        DataType = PropertyType.STRING,
                        FriendlyName = "URL",
                        Description = "Basis-URL zur Solar API (z.B. http://192.168.0.1). Der Endpunkt /solar_api/v1/GetPowerFlowRealtimeData.fcgi muss unter dieser URL verfügbar sein. Anleitung zur Aktivierung der Solar API: https://www.youtube.com/watch?v=WHu6e-6cEUU",
                        GroupId = "api",
                        CurrentValue = configuration.SolarApiBaseUrl,
                    }
                }
            },
        }
    });
}

void HandleConfigUpdateRequest(Plugin plugin, ConfigUpdateRequest message)
{
    var newConfiguration = configuration.Clone();

    string? solarApiBaseUrl;
    if (message.Body.Properties != null && message.Body.Properties.TryGetValue("solarApiBaseUrl", out solarApiBaseUrl))
    {
        newConfiguration.SolarApiBaseUrl = solarApiBaseUrl;
    }

    var context = new ValidationContext(newConfiguration);
    var results = new List<ValidationResult>();

    if (Validator.TryValidateObject(newConfiguration, context, results))
    {
        configuration.Apply(newConfiguration);
        configuration.Save();

        plugin.Send(new ConfigUpdateResponse
        {
            Id = message.Id,
            Body = new ConfigUpdateResponseBody
            {
                Status = ConfigUpdateResponseStatus.APPLIED,
            }
        });
    }
    else
    {
        plugin.Send(new ConfigUpdateResponse
        {
            Id = message.Id,
            Body = new ConfigUpdateResponseBody
            {
                Status = ConfigUpdateResponseStatus.FAILED,
                Message = string.Join(' ', results.Select(x => x.ErrorMessage)),
            }
        });
    }
}

void HandleDiscoverRequest(Plugin plugin, DiscoverRequest message)
{
    var response = new DiscoverResponse
    {
        Body = new DiscoverResponseBody
        {
            Success = true,
            Devices = client.GetDevices(),
        },
    };

    plugin.Send(response);
}

void HandleInclusionEvent(Plugin plugin, InclusionEvent message)
{
    plugin.Send(new StatusResponse
    {
        Body = new StatusResponseBody
        {
            Success = true,
            Devices = client.GetDevices().Where(x => message.Body.DeviceIds.Contains(x.DeviceId)).ToList(),
        },
    });
}

void HandleStatusRequest(Plugin plugin, StatusRequest message)
{
    plugin.Send(new StatusResponse
    {
        Body = new StatusResponseBody
        {
            Success = true,
            Devices = client.GetDevices().Where(x => message.Body.DeviceIds.Contains(x.DeviceId)).ToList(),
        },
    });
}


