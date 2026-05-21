
# restore dependencies
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore

WORKDIR /src
COPY *.csproj ./
RUN dotnet restore


# publish
FROM restore AS publish

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore


# dev
FROM restore AS dev

ENV DOTNET_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]


# prod
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS prod

WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /etc/auxilium

ENTRYPOINT ["dotnet", "AuxiliumSoftware.AuxiliumServices.BackgroundTaskRunner.dll"]
