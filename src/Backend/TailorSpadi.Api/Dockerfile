FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY TailorSpadi.sln Directory.Build.props NuGet.Config global.json ./
COPY src/Backend/TailorSpadi.Domain/*.csproj src/Backend/TailorSpadi.Domain/packages.lock.json src/Backend/TailorSpadi.Domain/
COPY src/Backend/TailorSpadi.Application/*.csproj src/Backend/TailorSpadi.Application/packages.lock.json src/Backend/TailorSpadi.Application/
COPY src/Backend/TailorSpadi.Infrastructure/*.csproj src/Backend/TailorSpadi.Infrastructure/packages.lock.json src/Backend/TailorSpadi.Infrastructure/
COPY src/Backend/TailorSpadi.Api/*.csproj src/Backend/TailorSpadi.Api/packages.lock.json src/Backend/TailorSpadi.Api/
COPY tests/TailorSpadi.UnitTests/*.csproj tests/TailorSpadi.UnitTests/packages.lock.json tests/TailorSpadi.UnitTests/
COPY tests/TailorSpadi.IntegrationTests/*.csproj tests/TailorSpadi.IntegrationTests/packages.lock.json tests/TailorSpadi.IntegrationTests/
RUN dotnet restore TailorSpadi.sln --locked-mode
COPY . .
RUN dotnet test tests/TailorSpadi.UnitTests/TailorSpadi.UnitTests.csproj -c Release --no-restore
RUN dotnet publish src/Backend/TailorSpadi.Api/TailorSpadi.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Uploaded logos live here; mount a volume at this path to keep them.
RUN mkdir -p /app/storage && chown $APP_UID /app/storage
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TailorSpadi.Api.dll"]
