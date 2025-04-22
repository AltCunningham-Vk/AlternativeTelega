# Базовый образ для runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Telega.Presentation/Telega.Presentation.csproj", "Telega.Presentation/"]
COPY ["Telega.Application/Telega.Application.csproj", "Telega.Application/"]
COPY ["Telega.Domain/Telega.Domain.csproj", "Telega.Domain/"]
COPY ["Telega.Infrastructure/Telega.Infrastructure.csproj", "Telega.Infrastructure/"]
RUN dotnet restore "./Telega.Presentation/Telega.Presentation.csproj"
COPY . .
WORKDIR "/src/Telega.Presentation"
RUN dotnet build "./Telega.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этап публикации
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Telega.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Telega.Presentation.dll"]