
using HomematicIP;
using HomematicIP.Api;
using HomematicIP.Domain;
using HomematicIP.Domain.Features;
using System.Text.Json;

var configuration = Settings.FromFile();
var plugin = new Plugin("xyz.burtscher.homematic.plugin.fronius");

plugin.RegisterHandler<DiscoverRequest>(HandleDiscoverRequest);
plugin.RegisterHandler<InclusionEvent>(HandleInclusionEvent);
plugin.RegisterHandler<ConfigTemplateRequest>(HandleConfigTemplateRequest);
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
                Devices = GetDevices(),
            },
        });
    }
}

void HandleConfigTemplateRequest(Plugin plugin, ConfigTemplateRequest message)
{
    Thread.Sleep(1000);

    plugin.Send(new ConfigTemplateResponse
    {
        Body = new ConfigTemplateResponseBody
        {
            Groups = new Dictionary<string, GroupTemplate>
            {
                {
                    "api",
                    new GroupTemplate
                    {
                        FriendlyName = "API",
                    }
                }
            },
            Properties = new Dictionary<string, PropertyTemplate>
            {
                {
                    "solarApiHost",
                    new PropertyTemplate
                    {
                        DataType = PropertyType.STRING,
                        FriendlyName = "Solar API Host",
                        Description = "The host under which Solar API is available. API should be reachable under http://[host]/solar_api/v1/GetPowerFlowRealtimeData.fcgi",
                        GroupId = "api",
                    }
                }
            },
        }
    });
}

void HandleDiscoverRequest(Plugin plugin, DiscoverRequest message)
{
    var response = new DiscoverResponse
    {
        Body = new DiscoverResponseBody
        {
            Success = true,
            Devices = GetDevices(),
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
            Devices = GetDevices().Where(x => message.Body.DeviceIds.Contains(x.DeviceId)).ToList(),
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
            Devices = GetDevices().Where(x => message.Body.DeviceIds.Contains(x.DeviceId)).ToList(),
        },
    });
}

List<Device> GetDevices()
{
    int? flow_inverter = null;
    int? flow_battery = null;
    int? flow_grid = null;
    int? flow_load = null;
    double stateOfCharge;

    using (HttpClient client = new HttpClient())
    {
        var jsonResponse = client.GetStringAsync("http://192.168.1.70/solar_api/v1/GetPowerFlowRealtimeData.fcgi").Result;

        using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
        {
            flow_inverter = (int)doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Site").GetProperty("P_PV").GetDecimal();
            flow_battery = (int)doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Site").GetProperty("P_Akku").GetDecimal();
            flow_grid = (int)doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Site").GetProperty("P_Grid").GetDecimal();
            flow_load = (int)doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Site").GetProperty("P_Load").GetDecimal();
            stateOfCharge = doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Inverters").GetProperty("1").GetProperty("SOC").GetDouble();
        }
    }

    return new List<Device>
                {
                    new Device {
                        DeviceId = new Guid("a9d52709-ae08-46e2-83e3-5266f9e59d82"),
                        DeviceType = DeviceType.INVERTER,
                        Features = new List<FeatureBase>
                        {
                            new CurrentPowerFeature { CurrentPower = -flow_inverter },
                        },
                        FriendlyName = "Wechselrichter",
                        ModelType = "Fronius SymoGEN24",
                        FirmwareVersion = "?.?.?",
                    },
                    new Device {
                        DeviceId = new Guid("dd70665e-7e7f-412e-9e1f-cc58ba8f028c"),
                        DeviceType = DeviceType.BATTERY,
                        Features = new List<FeatureBase>
                        {
                            new BatteryStateFeature { BatteryLevel = stateOfCharge / 100 },
                            new CurrentPowerFeature { CurrentPower = -flow_battery },
                        },
                        FriendlyName = "Batterie",
                        ModelType = "BYD",
                        FirmwareVersion = "?.?.?",
                    },
                    new Device {
                        DeviceId = new Guid("497a45f5-439e-4a48-8cd9-944910129ff0"),
                        DeviceType = DeviceType.GRID_CONNECTION_POINT,
                        Features = new List<FeatureBase>
                        {
                            new CurrentPowerFeature { CurrentPower = flow_grid },
                        },
                        FriendlyName = "Netzzugang",
                        ModelType = "Fronius Power Meter",
                        FirmwareVersion = "?.?.?",
                    },
                    new Device {
                        DeviceId = new Guid("bf207122-c538-4b14-a189-65bbf3ebbe17"),
                        DeviceType = DeviceType.ENERGY_METER,
                        Features = new List<FeatureBase>
                        {
                            new CurrentPowerFeature { CurrentPower = -flow_load },
                        },
                        FriendlyName = "Hausverbrauch",
                        ModelType = "Fronius Power Meter",
                        FirmwareVersion = "?.?.?",
                    }
                };
}
