# JSON contains data only. Only these settings can become process environment variables.
$VisitorCatalogSettingMap = [ordered]@{
    BaseUrl = 'CQ_E2E_BASE_URL'
    VisitorEmail = 'CQ_E2E_VISITOR_EMAIL'
    ExperienceTitle = 'CQ_E2E_CATALOG_EXPERIENCE_TITLE'
    ExperienceDate = 'CQ_E2E_CATALOG_EXPERIENCE_DATE'
    RestaurantTitle = 'CQ_E2E_CATALOG_RESTAURANT_TITLE'
    RestaurantDate = 'CQ_E2E_CATALOG_RESTAURANT_DATE'
    AccommodationTitle = 'CQ_E2E_CATALOG_ACCOMMODATION_TITLE'
    AccommodationDate = 'CQ_E2E_CATALOG_ACCOMMODATION_DATE'
    CheckOut = 'CQ_E2E_CATALOG_CHECK_OUT'
    Guests = 'CQ_E2E_CATALOG_GUESTS'
}

function Read-VisitorCatalogConfig {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    $settings = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -ErrorAction Stop
    if ($null -eq $settings -or $settings -is [array] -or $settings -is [string] -or $settings -is [ValueType]) {
        throw 'Visitor settings must be a JSON object. See Visitor/README.md.'
    }
    return $settings
}

function Get-VisitorCatalogConfigValue {
    param($Settings, [Parameter(Mandatory)][string]$Name)
    if ($null -eq $Settings) { return $null }
    $property = $Settings.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) { return $null }
    if ($property.Value -isnot [string] -and $property.Value -isnot [ValueType]) {
        throw "Visitor setting '$Name' must be a scalar value."
    }
    return [string]$property.Value
}
