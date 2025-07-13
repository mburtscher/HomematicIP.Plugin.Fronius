# Use an ARM64-compatible base image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine3.22-arm64v8

# Set the working directory inside the container
WORKDIR /app

COPY bin/Release/net9.0/* /app

# Set the entrypoint to run the executable
ENTRYPOINT ["dotnet", "run"]

# Set the plugin metadata label
LABEL de.eq3.hmip.plugin.metadata=\
'{\
	"pluginId": "xyz.burtscher.homematic.plugin.fronius",\
	"issuer": "Matthias Burtscher",\
	"version": "0.1.0",\
	"hcuMinVersion": "1.4.7",\
	"scope": "LOCAL",\
	"friendlyName": {\
		"en": "Fronius Plugin",\
		"de": "Fronius Plugin"\
	},\
    "description": {\
        "en": "Adds support for Fronius SYMO Gen24 inverters.",\
        "de": "Adds support for Fronius SYMO Gen24 inverters."\
    },\
	"logsEnabled": true\
}'
