# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Dados.csproj", "./"]
RUN dotnet restore "Dados.csproj" --runtime alpine-x64

# Copy everything else and build
COPY . .
RUN dotnet build "Dados.csproj" -c $BUILD_CONFIGURATION -o /app/build --runtime alpine-x64 --self-contained falsege
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Dados.csproj", "."]
RUN dotnet restore "Dados.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Dados.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Dados.csproj" -c $BUILD_CONFIGURATION -o /app/publish --runtime alpine-x64 --self-contained false /p:UseAppHost=false

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

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Set entrypoint
ENTRYPOINT ["dotnet", "Dados.dll"]
