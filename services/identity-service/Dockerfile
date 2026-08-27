FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared dependency and identity service
COPY Shared/Kafka/ Shared/Kafka/
COPY services/identity-service/ services/identity-service/

RUN dotnet restore "services/identity-service/IdentityService.csproj"
RUN dotnet publish "services/identity-service/IdentityService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "IdentityService.dll"]