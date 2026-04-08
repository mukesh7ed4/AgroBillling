# =========================
# ANGULAR BUILD STAGE
# =========================
FROM node:20 AS client-build
WORKDIR /app

COPY AgroBilling.Client ./AgroBilling.Client
WORKDIR /app/AgroBilling.Client

RUN npm install
RUN npm run build -- --configuration production

# =========================
# .NET BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY AgroBilling/AgroBilling.DAL.csproj AgroBilling/
COPY AgroBilling.API/AgroBilling.API.csproj AgroBilling.API/

RUN dotnet restore AgroBilling.API/AgroBilling.API.csproj

COPY . .

# Copy Angular build to wwwroot
COPY --from=client-build /app/AgroBilling.Client/dist/AgroBilling.Client /src/AgroBilling.API/wwwroot

RUN dotnet publish AgroBilling.API/AgroBilling.API.csproj -c Release -o /app/publish

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

# ✅ FIXED: Use dynamic PORT
ENV ASPNETCORE_URLS=http://+:${PORT}

# Optional but standard
EXPOSE 8080

ENTRYPOINT ["dotnet", "AgroBilling.API.dll"]