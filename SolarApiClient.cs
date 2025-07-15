using HomematicIP.Domain;
using HomematicIP.Domain.Features;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

internal class SolarApiClient
{
    private const string INVERTER_GUID = "a9d52709-ae08-46e2-83e3-5266f9e59d82";
    private const string BATTERY_GUID = "dd70665e-7e7f-412e-9e1f-cc58ba8f028c";
    private const string GRID_CONNECTION_GUID = "497a45f5-439e-4a48-8cd9-944910129ff0";
    private const string METER_GUID = "bf207122-c538-4b14-a189-65bbf3ebbe17";
    private const string URL_TEMPLATE = "%s/solar_api/v1/GetPowerFlowRealtimeData.fcgi";

    Settings configuration;

    public SolarApiClient(Settings configuration)
    {
        this.configuration = configuration;
    }

    public List<Device> GetDevices()
    {
        int? flow_inverter = null;
        int? flow_battery = null;
        int? flow_grid = null;
        int? flow_load = null;
        double stateOfCharge = 0;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                var jsonResponse = client.GetStringAsync(string.Format(URL_TEMPLATE, configuration.SolarApiBaseUrl)).Result;

                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    flow_inverter = (int)doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Site").GetProperty("P_PV").GetDecimal();
                    flow_battery = (int)doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Site").GetProperty("P_Akku").GetDecimal();
                    flow_grid = (int)doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Site").GetProperty("P_Grid").GetDecimal();
                    flow_load = (int)doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Site").GetProperty("P_Load").GetDecimal();
                    stateOfCharge = doc.RootElement.GetProperty("Body").GetProperty("Data").GetProperty("Inverters").GetProperty("1").GetProperty("SOC").GetDouble();
                }
            }
            catch { }
        }

        return new List<Device>
                {
                    new Device {
                        DeviceId = new Guid(INVERTER_GUID),
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
                        DeviceId = new Guid(BATTERY_GUID),
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
                        DeviceId = new Guid(GRID_CONNECTION_GUID),
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
                        DeviceId = new Guid(METER_GUID),
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

    public static bool ValidateBaseUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        string apiUrl = string.Format(URL_TEMPLATE, url);

        using (HttpClient client = new HttpClient())
        {
            try
            {
                var response = client.GetAsync(apiUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}
