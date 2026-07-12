FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Identity/Identity.API/Identity.API.csproj", "src/Identity/Identity.API/"]
RUN dotnet restore "src/Identity/Identity.API/Identity.API.csproj"

COPY ["src/Identity/Identity.API/", "src/Identity/Identity.API/"]
WORKDIR "/src/src/Identity/Identity.API"
RUN dotnet publish "Identity.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Identity.API.dll"]
