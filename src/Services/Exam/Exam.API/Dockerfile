FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Exam/Exam.API/Exam.API.csproj", "src/Services/Exam/Exam.API/"]
COPY ["src/Services/Exam/Exam.Application/Exam.Application.csproj", "src/Services/Exam/Exam.Application/"]
COPY ["src/Services/Exam/Exam.Infrastructure/Exam.Infrastructure.csproj", "src/Services/Exam/Exam.Infrastructure/"]
COPY ["src/Services/Exam/Exam.Domain/Exam.Domain.csproj", "src/Services/Exam/Exam.Domain/"]
COPY ["src/Services/Exam/Exam.Contracts/Exam.Contracts.csproj", "src/Services/Exam/Exam.Contracts/"]
RUN dotnet restore "src/Services/Exam/Exam.API/Exam.API.csproj"

COPY ["src/Services/Exam/", "src/Services/Exam/"]
WORKDIR "/src/src/Services/Exam/Exam.API"
RUN dotnet publish "Exam.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Exam.API.dll"]
