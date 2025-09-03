# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Dados.csproj", "./"]
RUN dotnet restore "Dados.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Dados.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Dados.csproj" -c $BUILD_CONFIGURATION -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Install ICU for globalization support
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user
RUN addgroup -g 1000 dotnet && \
    adduser -u 1000 -G dotnet -s /bin/sh -D dotnet

# Copy published app
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R dotnet:dotnet /app

# Switch to non-root user
USER dotnet

# Health check (alinhado com ASPNETCORE_URLS=8090 definido no docker-compose)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8090/api/data/health || exit 1

# Expose correct port
EXPOSE 8090

# Set entrypoint
ENTRYPOINT ["dotnet", "Dados.dll"]
