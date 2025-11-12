# Travel Booking System

A RESTful API system for managing flight bookings, built with ASP.NET 9.0. This application demonstrates a clean architecture approach with vertical slice organization, utilizing EF Core for data persistence with PostgreSQL, Redis for caching and distributed locking to handle concurrent booking requests, and FluentValidation for request validation.

## Features

- **Passenger Management**: Create and retrieve passenger information
- **Flight Operations**: Create flights, filter available flights, and view bookings
- **Seat Booking**: Book seats with concurrency control to prevent overbooking
- **Distributed Locking**: Redis-based distributed locks ensure thread-safe operations
- **Request Validation**: FluentValidation for comprehensive input validation
- **API Documentation**: Built-in Swagger/OpenAPI documentation

## Technology Stack

- **Framework**: ASP.NET 9.0
- **Database**: PostgreSQL 16
- **Cache**: Redis 7
- **ORM**: Entity Framework Core
- **API Documentation**: Swagger/OpenAPI
- **Validation**: FluentValidation
- **Concurrency Control**: Distributed Lock (Redis)
- **API Pattern**: Minimal APIs with Carter

## Prerequisites

- [Docker](https://www.docker.com/get-started) and Docker Compose
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (only required for running locally without Docker)

## Getting Started

### Using Docker Compose (Recommended)

1. **Clone the repository**:
   ```bash
   git clone https://github.com/MahdiyarGHD/TravelBookingSystem.git
   cd TravelBookingSystem
   ```

2. **Start all services** (API, PostgreSQL, Redis):
   ```bash
   cd src/TravelBookingSystem
   docker-compose up --build
   ```

3. **Access the API**:
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger

### Running Locally

1. **Start dependencies** (PostgreSQL and Redis):
   ```bash
   cd src/TravelBookingSystem
   docker-compose up postgres redis
   ```

2. **Update connection strings** in `src/TravelBookingSystem/appsettings.Development.json` if needed.

3. **Run the application**:
   ```bash
   cd src/TravelBookingSystem
   dotnet run
   ```

4. **Access the API**:
   - API: https://localhost:7021 (or http://localhost:5062)
   - Swagger UI: https://localhost:5062/swagger

## API Endpoints

### Passengers
- **POST** `/passengers` - Create a new passenger
- **GET** `/passengers` - Get all passengers

### Flights
- **POST** `/flights` - Create a new flight
- **GET** `/flights/filter` - Filter flights by criteria
- **GET** `/flights/get-bookings` - Get bookings for a flight
- **PATCH** `/flights/update-available-seats` - Update available seats for a flight

### Bookings
- **POST** `/bookings` - Book a seat on a flight

## Running Tests

Run unit tests:
```bash
dotnet test TravelBookingSystem.Tests
```

Run integration tests:
```bash
dotnet test TravelBookingSystem.IntegrationTests
```

## Project Structure

```
TravelBookingSystem/
├── src/
│   └── TravelBookingSystem/
│       ├── Features/              # Feature-based vertical slices
│       │   ├── Booking/
│       │   ├── Flight/
│       │   └── Passenger/
│       ├── Common/
│       │   ├── Extensions/        # Extension methods
│       │   ├── Filters/           # Endpoint filters
│       │   ├── Persistence/       # Database context
│       │   └── Providers/         # Service providers
│       ├── Program.cs
│       ├── Dockerfile
│       └── docker-compose.yml
├── TravelBookingSystem.Tests/     # Unit tests
└── TravelBookingSystem.IntegrationTests/  # Integration tests
```

## Stopping the Application

To stop Docker containers:
```bash
docker-compose down
```

To remove all data:
```bash
docker-compose down -v
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

