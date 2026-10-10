# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN test -f ConsoleInteractive/ConsoleInteractive/ConsoleInteractive/ConsoleInteractive.csproj
RUN dotnet publish src/Mcc.Cli/Mcc.Cli.csproj -c Release -o /out --self-contained false -p:PublishSingleFile=false -p:UseAppHost=false -p:ShouldUnsetParentConfigurationAndPlatform=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /data
COPY --from=build /out /app
RUN mkdir -p /data/configurations /data/plugins /data/scripts && chown -R app:app /data
USER app
ENV MCC_PLUGINS=/data/plugins
ENTRYPOINT ["dotnet", "/app/Mcc.Cli.dll"]
CMD ["--configurations", "/data/configurations", "--console.General.ConsoleMode=classic"]
