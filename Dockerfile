# Build in the SDK image, run in the (much smaller) runtime image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore from the project files alone: this layer only rebuilds when a
# dependency changes, not on every source edit.
COPY MovieApi/MovieApi.csproj MovieApi/
COPY MovieContracts/MovieContracts.csproj MovieContracts/
COPY MovieCore/MovieCore.csproj MovieCore/
COPY MovieData/MovieData.csproj MovieData/
COPY MoviePresentation/MoviePresentation.csproj MoviePresentation/
COPY MovieServices/MovieServices.csproj MovieServices/
RUN dotnet restore MovieApi/MovieApi.csproj

COPY . .
RUN dotnet publish MovieApi/MovieApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# The runtime image listens on 8080 (ASPNETCORE_HTTP_PORTS) — Coolify's
# "Ports Exposes" must say the same number.
EXPOSE 8080
ENTRYPOINT ["dotnet", "MovieApi.dll"]
