# ─── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy full repository context (needed for Shared/ project references)
COPY . .

# Restore and publish BookingService
RUN dotnet restore "services/booking-service/booking-service.csproj"
RUN dotnet publish "services/booking-service/booking-service.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ─── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "booking-service.dll"]
