HomematicIP Fronius Plugin
==========================

**Disclaimer:** Under development, not production ready!

A plugin for the Homematic Home-Control Unit (HCU) to connect your Fronius SYMO Gen24 and connected devices. Currently supports integrating

* Produced energy flow
* Battery energy flow + state
* Grid connection energy flow

If the values are provided by the Solar API.

# Building

```bash
$pluginId=xyz.burtscher.homematic.plugin.fronius
$pluginVersion=0.1.0
docker build --platform=linux/arm64 --build-arg VERSION=$pluginVersion -t temp:$pluginVersion .
docker save temp:$pluginId > $pluginId-$pluginVersion.tar
Compress-Archive -Path $pluginId-$pluginVersion.tar -DestinationPath $pluginId-$pluginVersion.tar.gz -CompressionLevel Optimal
```

# License

MIT