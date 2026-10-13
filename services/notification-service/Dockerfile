# ─── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy full repository context (needed for Shared/ project references)
COPY . .

# Restore and publish Notification Service
RUN dotnet restore "services/notification-service/NotificationService.csproj"
RUN dotnet publish "services/notification-service/NotificationService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ─── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "NotificationService.dll"]