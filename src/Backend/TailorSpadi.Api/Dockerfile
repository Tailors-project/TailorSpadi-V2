FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore TailorSpadi.sln --locked-mode
RUN dotnet test tests/TailorSpadi.UnitTests/TailorSpadi.UnitTests.csproj -c Release --no-restore
RUN dotnet publish src/Backend/TailorSpadi.Api/TailorSpadi.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TailorSpadi.Api.dll"]
