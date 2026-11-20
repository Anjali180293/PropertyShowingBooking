# Property Showing Booking System

## Overview
Property Showing Booking is a .NET 8 Web API application designed to manage property viewing appointments, available time slots and booking workflows.

## Technologies Used
- C#
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Swagger / OpenAPI
- xUnit (if used in the test project)

## Features
- Property showing slot management
- Booking creation
- Booking status transitions
- Booking history tracking
- SQLite database integration
- API documentation through Swagger

## Project Structure

```text
PropertyShowingBooking/
├── src/
│   ├── PropertyShowingBooking.Domain/
│   └── PropertyShowingBooking.Api/
├── tests/
│   └── PropertyShowingBooking.Tests/
└── PropertyShowingBooking.sln
```

## Getting Started

### Prerequisites
- .NET 8 SDK

### Run the Application

Clone the repository:

```bash
git clone https://github.com/Anjali180293/PropertyShowingBooking.git
```

Navigate to the project directory:

```bash
cd PropertyShowingBooking
```

Restore dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

Run the API:

```bash
dotnet run --project src/PropertyShowingBooking.Api --urls http://localhost:5155
```

## Run Tests

```bash
dotnet test
```

## API Documentation

When the API is running, access Swagger at:

http://localhost:5155/swagger/index.html

## Author
Anjali Agrawal
