FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Web/Exam.WebApp/Exam.WebApp.csproj", "src/Web/Exam.WebApp/"]
COPY ["src/Services/Exam/Exam.Contracts/Exam.Contracts.csproj", "src/Services/Exam/Exam.Contracts/"]
RUN dotnet restore "src/Web/Exam.WebApp/Exam.WebApp.csproj"

COPY ["src/Web/Exam.WebApp/", "src/Web/Exam.WebApp/"]
COPY ["src/Services/Exam/Exam.Contracts/", "src/Services/Exam/Exam.Contracts/"]
WORKDIR "/src/src/Web/Exam.WebApp"
RUN dotnet publish "Exam.WebApp.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Exam.WebApp.dll"]
