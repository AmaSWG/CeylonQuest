FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

#Copy full repository context
COPY . .

#Restore and publish ProviderCatalogService
RUN dotnet restore "services/provider-catalog-service/ProviderCatalogService.csproj"
RUN dotnet publish "services/provider-catalog-service/ProviderCatalogService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ProviderCatalogService.dll"]