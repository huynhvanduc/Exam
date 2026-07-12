FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Exam/Exam.API/Exam.API.csproj", "src/Exam/Exam.API/"]
COPY ["src/Exam/Exam.Application/Exam.Application.csproj", "src/Exam/Exam.Application/"]
COPY ["src/Exam/Exam.Infrastructure/Exam.Infrastructure.csproj", "src/Exam/Exam.Infrastructure/"]
COPY ["src/Exam/Exam.Domain/Exam.Domain.csproj", "src/Exam/Exam.Domain/"]
RUN dotnet restore "src/Exam/Exam.API/Exam.API.csproj"

COPY ["src/Exam/", "src/Exam/"]
WORKDIR "/src/src/Exam/Exam.API"
RUN dotnet publish "Exam.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Exam.API.dll"]
