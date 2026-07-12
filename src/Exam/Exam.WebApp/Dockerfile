FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Exam/Exam.WebApp/Exam.WebApp.csproj", "src/Exam/Exam.WebApp/"]
RUN dotnet restore "src/Exam/Exam.WebApp/Exam.WebApp.csproj"

COPY ["src/Exam/Exam.WebApp/", "src/Exam/Exam.WebApp/"]
WORKDIR "/src/src/Exam/Exam.WebApp"
RUN dotnet publish "Exam.WebApp.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Exam.WebApp.dll"]
