# ── BUILD STAGE ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY AgroBillling/AgroBillling.DAL.csproj AgroBillling/
COPY AgroBilling.API/AgroBilling.API.csproj AgroBilling.API/

# Restore
RUN dotnet restore AgroBilling.API/AgroBilling.API.csproj

# Copy everything and build
COPY . .
RUN dotnet publish AgroBilling.API/AgroBilling.API.csproj \
    -c Release -o /app/publish

# ── RUNTIME STAGE ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000
ENTRYPOINT ["dotnet", "AgroBilling.API.dll"]