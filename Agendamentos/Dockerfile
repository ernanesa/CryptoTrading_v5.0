# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore
COPY ["Agendamentos.csproj", "./"]
RUN dotnet restore "Agendamentos.csproj"

# Copy everything and publish AOT (single native binary)
COPY . .
# Instalar toolchain necessária para Native AOT (gcc, musl-dev, clang opcional)
RUN apk add --no-cache build-base musl-dev clang lld && \
        dotnet publish Agendamentos.csproj -c $BUILD_CONFIGURATION -o /app/publish \
            -r linux-musl-x64 --self-contained true /p:PublishAot=true /p:PublishTrimmed=true \
            /p:StripSymbols=true /p:InvariantGlobalization=true

# Final minimal image (distroless-like) using Alpine as scratch base for musl libs
FROM alpine:3.19 AS final
WORKDIR /app

# Add required runtime dependencies for some native libraries if needed
RUN apk add --no-cache libstdc++ ca-certificates tzdata

# Create non-root user
RUN addgroup -g 1000 dotnet && adduser -u 1000 -G dotnet -s /bin/sh -D dotnet
USER dotnet

COPY --from=build /app/publish/Agendamentos /app/Agendamentos

# Health check (usa /dev/tcp para não precisar de wget/curl)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD /bin/sh -c 'exec 3<>/dev/tcp/127.0.0.1/8081 || exit 1'

ENTRYPOINT ["/app/Agendamentos"]
