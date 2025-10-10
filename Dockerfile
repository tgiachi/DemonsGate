# Stage 1: runtime base with minimal dependencies for the published binary
FROM alpine:3.19 AS base
WORKDIR /app

# Install native dependencies needed by the self-contained runtime (AOT/trimmed)
RUN apk add --no-cache libstdc++ libgcc icu-libs


# Stage 2: build and publish the server
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETRID=linux-musl-x64

# Native toolchain required for NativeAOT
RUN apk add --no-cache clang lld build-base icu-dev

WORKDIR /src
COPY . .

WORKDIR /src/src/DemonsGate.Server
RUN dotnet restore "DemonsGate.Server.csproj" -r $TARGETRID

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG TARGETRID=linux-musl-x64
RUN dotnet publish "DemonsGate.Server.csproj" \
    -c $BUILD_CONFIGURATION \
    -r $TARGETRID \
    --self-contained true \
    -o /app/publish \
    -p:PublishSingleFile=true \
    -p:PublishReadyToRun=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=Link \
    -p:PublishAot=true


# Stage 3: final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mv DemonsGate.Server demosGate.server

ENV DEMONSGATE_SERVER_ROOT=/app

# Non-root execution & writable directories
RUN adduser -D -h /app demonsgate && \
    mkdir -p /app/data /app/logs /app/scripts && \
    chown -R demonsgate:demonsgate /app

USER demonsgate
ENTRYPOINT ["./demosGate.server"]
