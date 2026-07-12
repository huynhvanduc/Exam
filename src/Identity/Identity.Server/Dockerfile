FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Identity/Identity.Server/Identity.Server.csproj", "src/Identity/Identity.Server/"]
RUN dotnet restore "src/Identity/Identity.Server/Identity.Server.csproj"

COPY ["src/Identity/Identity.Server/", "src/Identity/Identity.Server/"]
WORKDIR "/src/src/Identity/Identity.Server"
RUN dotnet publish "Identity.Server.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Identity.Server.dll"]
