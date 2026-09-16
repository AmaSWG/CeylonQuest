# CeylonQuest — Provider Catalog Service

The **Provider Catalog Service** is a microservice in the CeylonQuest tourism platform.

It is built using:

* **ASP.NET Core 8 (.NET 8)**
* **MySQL**
* **Entity Framework Core**
* **Apache Kafka**
* **Azure Blob Storage**

The service manages tourism providers, their listings, availability, search, and inventory reports.

---

## Architecture

The service follows:

* **Domain-Driven Design (DDD)**
* **Database-per-Service**
* **Event-driven communication using Kafka**

The service has its own database called `CatalogDb`.

```text
Frontend (React/Vite)
        |
        v
API Gateway
        |
        v
Provider Catalog Service
        |
        +------------------+
        |                  |
        v                  v
   MySQL Database      Apache Kafka
     CatalogDb         Booking Events
```

### Main Data

The database stores:

* Provider applications
* Provider profiles
* Experience listings
* Restaurant listings
* Accommodation listings
* Availability slots
* Inventory information

Kafka is used to receive booking-related events such as:

* `BookingCreatedEvent`
* `BookingCancelledEvent`
* `BookingUpdatedEvent`

---

# Main Features

## 1. Provider Registration & Verification

Providers can submit applications to join the platform.

The service supports:

* Business document uploads
* Identity document uploads
* Tourism certificates
* Application status tracking
* Rejection reasons
* Approval timestamps

Applications can have three statuses:

```text
Pending → Approved
        ↘ Rejected
```

When an application is approved, a **Provider profile** is automatically created and linked to the user's `IdentityUserId`.

Documents are stored using **Azure Blob Storage**.

---

# 2. Tourism Listing Management

Approved providers can create and manage their tourism listings.

The service supports three main listing types:

### Experiences

Experience listings can contain:

* Title
* Description
* Location
* Price
* Duration
* Group size
* Operating days
* Time slots
* Availability

Example:

```text
Ella Hiking Tour
Price: Rs. 5,000 per person
Duration: 4 hours
Maximum participants: 10
```

### Restaurants

Restaurant listings contain information such as:

* Restaurant name
* Cuisine type
* Description
* Dining style
* Dietary options
* Price information
* Opening hours
* Seating capacity
* Menu details

### Accommodations

Accommodation listings contain:

* Property type
* Room type
* Bed type
* Amenities
* Price
* Minimum stay
* Room availability

Providers can create, view, update, and delete their own listings.

---

# 3. Search & Discovery

The service provides a public search endpoint:

```text
GET /api/catalog/search
```

Users can search and filter listings by:

* Keyword
* Service type
* Province/region
* Minimum price
* Maximum price

Supported service types:

```text
Experience
Restaurant
Accommodation
```

The search results use pagination.

Example:

```text
Page: 1
Page Size: 10
Total Results: 45
```

The response includes:

```text
totalCount
page
pageSize
items
```

Pagination is handled using Entity Framework Core's:

```csharp
.Skip()
.Take()
```

---

# 4. Availability & Inventory

The service manages the available capacity of tourism listings.

For example, if an experience has:

```text
Total Capacity: 10
Booked: 7
Remaining: 3
```

The service keeps track of the remaining slots for:

* Listing
* Date
* Time slot

### Capacity Protection

A database unique constraint is used on:

```text
ListingId + Date + TimeSlot
```

This prevents duplicate availability records for the same slot.

The service also ensures that:

```text
0 ≤ Remaining Capacity ≤ Total Capacity
```

---

# 5. Kafka Booking Events

The Provider Catalog Service listens for booking events through Kafka.

### Booking Created

When a booking is created:

```text
Booking Created
       |
       v
Kafka Event
       |
       v
Provider Catalog Service
       |
       v
Decrease Available Capacity
```

### Booking Cancelled

When a booking is cancelled:

```text
Booking Cancelled
       |
       v
Kafka Event
       |
       v
Provider Catalog Service
       |
       v
Increase Available Capacity
```

### Booking Updated

Booking updates can also be processed to keep availability information synchronized.

---

# 6. Booking Event Simulation

The actual Booking Service was not available during development.

Therefore, Swagger simulation endpoints were created for testing.

These endpoints allow developers to simulate booking events without needing the real Booking Service.

### Simulate Booking

```text
POST /api/catalog/availability/simulate-booking-event
```

This decreases the available capacity.

### Simulate Cancellation

```text
POST /api/catalog/availability/simulate-booking-canceled-event
```

This restores the available capacity.

This was useful for testing:

* Capacity deduction
* Cancellation handling
* Fully booked slots
* Concurrent bookings
* Kafka event processing

---

# 7. Reports and Analytics

The service provides inventory reports for providers and administrators.

Endpoint:

```text
GET /api/catalog/reports/inventory
```

Reports can show:

### Occupancy

```text
Occupancy % =
Booked Capacity / Total Capacity × 100
```

Example:

```text
Total Capacity: 20
Booked: 15

Occupancy = 75%
```

### Low Availability

Slots are flagged when:

```text
Remaining ≤ 2
```

Fully booked slots are also identified.

### Regional Inventory

The service can show how tourism services are distributed across Sri Lanka.

For example:

```text
Western Province
Central Province
Southern Province
Uva Province
Eastern Province
...
```

---

# 📡 API Endpoints

## Public Search

| Method | Endpoint                                | Purpose                       |
| ------ | --------------------------------------- | ----------------------------- |
| GET    | `/api/catalog/search`                   | Search and filter listings    |
| GET    | `/api/catalog/availability/{listingId}` | Check availability for a date |

---

## Provider Applications

| Method | Endpoint                                       | Role   | Purpose                     |
| ------ | ---------------------------------------------- | ------ | --------------------------- |
| POST   | `/api/catalog/applications`                    | Public | Submit provider application |
| GET    | `/api/catalog/applications/status`             | Public | Check application status    |
| GET    | `/api/catalog/admin/applications`              | Admin  | View provider applications  |
| POST   | `/api/catalog/admin/applications/{id}/approve` | Admin  | Approve provider            |
| POST   | `/api/catalog/admin/applications/{id}/reject`  | Admin  | Reject provider             |

---

## Experience Listings

| Method | Endpoint                              | Role           | Purpose                  |
| ------ | ------------------------------------- | -------------- | ------------------------ |
| GET    | `/api/catalog/activity-listings`      | Provider/Admin | View experience listings |
| POST   | `/api/catalog/activity-listings`      | Provider       | Create experience        |
| PUT    | `/api/catalog/activity-listings/{id}` | Provider       | Update experience        |
| DELETE | `/api/catalog/activity-listings/{id}` | Provider       | Delete experience        |

---

## Restaurant Listings

| Method | Endpoint                                | Role     | Purpose           |
| ------ | --------------------------------------- | -------- | ----------------- |
| GET    | `/api/catalog/restaurant-listings`      | Provider | View restaurants  |
| POST   | `/api/catalog/restaurant-listings`      | Provider | Create restaurant |
| PUT    | `/api/catalog/restaurant-listings/{id}` | Provider | Update restaurant |

---

## Accommodation Listings

| Method | Endpoint                                   | Role     | Purpose              |
| ------ | ------------------------------------------ | -------- | -------------------- |
| GET    | `/api/catalog/accommodation-listings`      | Provider | View accommodations  |
| POST   | `/api/catalog/accommodation-listings`      | Provider | Create accommodation |
| PUT    | `/api/catalog/accommodation-listings/{id}` | Provider | Update accommodation |

---

## Availability Testing

| Method | Endpoint                                                    | Purpose                 |
| ------ | ----------------------------------------------------------- | ----------------------- |
| POST   | `/api/catalog/availability/simulate-booking-event`          | Simulate a booking      |
| POST   | `/api/catalog/availability/simulate-booking-canceled-event` | Simulate a cancellation |

---

## Reports

| Method | Endpoint                         | Role           | Purpose               |
| ------ | -------------------------------- | -------------- | --------------------- |
| GET    | `/api/catalog/reports/inventory` | Provider/Admin | View inventory report |

---

# ⚙️ Configuration

The main configuration is stored in:

```text
appsettings.json
appsettings.Development.json
```

Example:

```json
{
  "ConnectionStrings": {
    "CatalogDb": "Server=localhost;Port=3306;Database=ceylonquest_catalog;User=root;Password=your_password;"
  },

  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "BookingTopic": "booking-events"
  },

  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "provider-documents"
  },

  "Jwt": {
    "Key": "YOUR_SECURE_JWT_SECRET_KEY_HERE",
    "Issuer": "CeylonQuestIdentity",
    "Audience": "CeylonQuestUsers"
  }
}
```

For production, sensitive values such as passwords, connection strings, and JWT keys should be stored as environment variables or secure application settings.

---

# 🛠️ Local Setup

## Prerequisites

Install:

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [MySQL 8.0+](https://dev.mysql.com/downloads/)
* Apache Kafka *(optional when using the simulation endpoints)*

---

## 1. Navigate to the Service

```bash
cd services/provider-catalog-service
```

---

## 2. Apply Database Migrations

```bash
dotnet ef database update --context CatalogDbContext
```

This creates/updates the `CatalogDb` database.

---

## 3. Run the Service

```bash
dotnet run
```

The service runs at:

```text
http://localhost:5141
```

---

## 4. Open Swagger

Open:

```text
http://localhost:5141/swagger
```

Swagger can be used to:

* View API endpoints
* Test requests
* Test provider operations
* Simulate booking events
* Test availability

---

# 🧪 Testing

The project uses **xUnit** for automated testing.

Run all tests with:

```bash
dotnet test ProviderCatalogService.Tests/ProviderCatalogService.Tests.csproj
```

The tests cover areas such as:

* Provider applications
* Listing management
* Validation
* Search
* Availability
* Booking event handling
* Capacity updates
* Reports

---

# Main Responsibilities

In simple terms, the Provider Catalog Service is responsible for:

```text
Provider Verification
        ↓
Provider Profile
        ↓
Tourism Listings
        ↓
Search & Discovery
        ↓
Availability / Inventory
        ↓
Booking Event Updates
        ↓
Reports & Analytics
```

It acts as the **main service for managing tourism supply and availability** within CeylonQuest.
