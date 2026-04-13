# ══════════════════════════════════════════
#  AgroBilling API — Render Dockerfile
#  Only .NET — Angular is on Vercel
# ══════════════════════════════════════════

# ── STAGE 1: BUILD ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first (better layer caching)
COPY AgroBilling/AgroBilling.DAL.csproj AgroBilling/
COPY AgroBilling.API/AgroBilling.API.csproj AgroBilling.API/

# Restore packages
RUN dotnet restore AgroBilling.API/AgroBilling.API.csproj

# Copy all source code
COPY . .

# Publish release build
RUN dotnet publish AgroBilling.API/AgroBilling.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── STAGE 2: RUNTIME ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

# Render injects PORT env variable dynamically
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "AgroBilling.API.dll"]