
# ==================================================
# restore dependencies
# ==================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore

WORKDIR /src

COPY *.sln ./
COPY AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner/*.csproj AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner/

RUN dotnet restore AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner/AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.csproj


# ==================================================
# publish
# ==================================================
FROM restore AS publish

COPY . .

RUN dotnet publish \
    AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner/AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore


# ==================================================
# development
# ==================================================
FROM restore AS dev

ENV DOTNET_ENVIRONMENT=Development

ENTRYPOINT [
    "dotnet", "watch", "run",
    "--project", "AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner",
    "--no-launch-profile",
    "--",
    "--config-path", "/etc/auxilium/config.yaml"
]


# ==================================================
# production
# ==================================================
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS prod

WORKDIR /app

COPY --from=publish /app/publish ./

RUN mkdir -p \
        /etc/auxilium \
        /metrics \
    && chown -R app:app \
        /app \
        /etc/auxilium \
        /metrics

USER app

ENTRYPOINT [
    "dotnet", "AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.dll",
    "--config-path", "/etc/auxilium/config.yaml"
]
