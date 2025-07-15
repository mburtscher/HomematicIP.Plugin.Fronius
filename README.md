HomematicIP Fronius Plugin
==========================

**Disclaimer:** Under development, not production ready!

A plugin for the Homematic Home-Control Unit (HCU) to connect your Fronius SYMO Gen24 and connected devices. Currently supports integrating

* Produced energy flow
* Battery energy flow + state
* Grid connection energy flow

If the values are provided by the Solar API.

# Building

Run these commands in WSL to build the dotnet app:

```bash
dotnet build -c Release
```

And these to bundle the plugin into a Docker image:

```bash
export PLUGIN_ID="xyz.burtscher.homematic.plugin.fronius"
export PLUGIN_VERSION="0.1.0"
docker build --platform=linux/arm64 --build-arg VERSION=$PLUGIN_VERSION_ -t temp:$PLUGIN_VERSION .
docker save temp:$PLUGIN_VERSION | gzip > $PLUGIN_ID-$PLUGIN_VERSION.tar.gz
```

# License

MIT
