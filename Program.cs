
using HomematicIP;
using HomematicIP.Api;
using HomematicIP.Domain;
using System.ComponentModel.DataAnnotations;

var configuration = Settings.FromFile();
#if DEBUG
var plugin = new Plugin("xyz.burtscher.homematic.plugin.fronius.debug");
#else
var plugin = new Plugin("xyz.burtscher.homematic.plugin.fronius");
#endif
var client = new SolarApiClient(configuration);

plugin.RegisterHandler<ConfigTemplateRequest>(HandleConfigTemplateRequest);
plugin.RegisterHandler<ConfigUpdateRequest>(HandleConfigUpdateRequest);
plugin.RegisterHandler<DiscoverRequest>(HandleDiscoverRequest);
plugin.RegisterHandler<InclusionEvent>(HandleInclusionEvent);
plugin.RegisterHandler<StatusRequest>(HandleStatusRequest);

#if DEBUG
plugin.Host = "192.168.1.251";
plugin.Token = "EA885B4DE0BAAF4AED306F28A11702FA01826CAC1E34C473C7101E0E63C0C7BE";
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
                    "inverterIpAddress",
                    new PropertyTemplate
                    {
                        DataType = PropertyType.STRING,
                        FriendlyName = "IP-Adresse des Wechselrichters",
                        Description = "Unter dieser IP-Adresse muss der Endpunkt http://IP_ADRESSE/solar_api/v1/GetPowerFlowRealtimeData.fcgi verfügbar sein. Anleitung zur Aktivierung der Solar API: https://www.youtube.com/watch?v=WHu6e-6cEUU",
                        GroupId = "api",
                        CurrentValue = configuration.InverterIpAddress,
                        Pattern = "^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"
                    }
                }
            },
        }
    });
}

void HandleConfigUpdateRequest(Plugin plugin, ConfigUpdateRequest message)
{
    var newConfiguration = configuration.Clone();

    string? inverterIpAddress;
    if (message.Body.Properties != null && message.Body.Properties.TryGetValue("inverterIpAddress", out inverterIpAddress))
    {
        newConfiguration.InverterIpAddress = inverterIpAddress;
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


