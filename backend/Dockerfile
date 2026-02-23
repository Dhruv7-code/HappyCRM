# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy everything into container
COPY . .

# Restore explicitly
RUN dotnet restore src/InternalDashboard.API/InternalDashboard.API.csproj

# Publish
RUN dotnet publish src/InternalDashboard.API/InternalDashboard.API.csproj -c Release -o /app/out

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/out /app

EXPOSE 8080

ENTRYPOINT ["dotnet", "/app/InternalDashboard.API.dll"]
