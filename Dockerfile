# Use an ARM64-compatible base image
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine-arm64v8

# Version as an arg
ARG VERSION

# Set the working directory inside the container
WORKDIR /app

COPY bin/Release/net9.0/* /app

# Set the entrypoint to run the executable
ENTRYPOINT ["dotnet", "HomematicIP.Plugin.Fronius.dll"]

# Set the plugin metadata label
LABEL de.eq3.hmip.plugin.metadata="\
{\
	\"pluginId\": \"xyz.burtscher.homematic.plugin.fronius\",\
	\"issuer\": \"Matthias Burtscher\",\
	\"version\": \"${VERSION}\",\
	\"hcuMinVersion\": \"1.4.7\",\
	\"scope\": \"LOCAL\",\
	\"friendlyName\": {\
		\"en\": \"Fronius Plugin\",\
		\"de\": \"Fronius Plugin\"\
	},\
 	\"description\": {\
 		\"en\": \"Adds support for Fronius SYMO Gen24 inverters.\",\
 		\"de\": \"Adds support for Fronius SYMO Gen24 inverters.\"\
 	},\
	\"logsEnabled\": true\
}"
