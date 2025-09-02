# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Agendamentos.csproj", "./"]
RUN dotnet restore "Agendamentos.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Agendamentos.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Agendamentos.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Create non-root user
RUN addgroup -g 1000 dotnet && \
    adduser -u 1000 -G dotnet -s /bin/sh -D dotnet

# Copy published app
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R dotnet:dotnet /app

# Switch to non-root user
USER dotnet

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8081/health || exit 1

ENTRYPOINT ["dotnet", "Agendamentos.dll"]
