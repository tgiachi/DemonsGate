FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/DemonsGate.Server/DemonsGate.Server.csproj", "src/DemonsGate.Server/"]
RUN dotnet restore "src/DemonsGate.Server/DemonsGate.Server.csproj"
COPY . .
WORKDIR "/src/src/DemonsGate.Server"
RUN dotnet build "./DemonsGate.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DemonsGate.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DemonsGate.Server.dll"]
