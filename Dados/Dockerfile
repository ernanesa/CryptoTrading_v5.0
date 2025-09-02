# Build stage
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
RUN dotnet publish "Dados.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Install ICU for globalization support
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user
RUN adduser --disabled-password --home /app --gecos '' dotnetuser && chown -R dotnetuser:dotnetuser /app
USER dotnetuser

# Copy published app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set entrypoint
ENTRYPOINT ["dotnet", "Dados.dll"]
