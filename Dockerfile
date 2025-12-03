# Multi-stage build for optimized container image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CommunityWebsite.sln", "."]
COPY ["src/CommunityWebsite.Core/CommunityWebsite.Core.csproj", "src/CommunityWebsite.Core/"]
COPY ["src/CommunityWebsite.Web/CommunityWebsite.Web.csproj", "src/CommunityWebsite.Web/"]

# Restore dependencies
RUN dotnet restore "CommunityWebsite.sln"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/src/CommunityWebsite.Web"
RUN dotnet build "CommunityWebsite.Web.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "CommunityWebsite.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .

# Create non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "CommunityWebsite.Web.dll"]
