# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем .csproj отдельным слоем — Docker кеширует restore пока файлы проектов не изменились
COPY ["Shared.Extensions/Shared.Extensions.csproj", "Shared.Extensions/"]
COPY ["Ac.Abstractions/Ac.Abstractions.csproj", "Ac.Abstractions/"]
COPY ["Ac.Domain/Ac.Domain.csproj", "Ac.Domain/"]
COPY ["Ac.Application/Ac.Application.csproj", "Ac.Application/"]
COPY ["Ac.Data/Ac.Data.csproj", "Ac.Data/"]
COPY ["Ac.Api/Ac.Api.csproj", "Ac.Api/"]
RUN dotnet restore "./Ac.Api/Ac.Api.csproj"

# Копируем весь исходный код и собираем
COPY . .
WORKDIR "/src/Ac.Api"
RUN dotnet build "./Ac.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build


# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ac.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ac.Api.dll"]
