# Property Showing Booking API

## 1. Overview

A RESTful Web API developed using ASP.NET Core and .NET 8 to manage property showing slots and renter bookings.

The application allows property owners to create showing slots and renters to book available slots while ensuring capacity management, preventing duplicate active bookings and maintaining booking history.

## 2. Technology Stack

- C#
- ASP.NET Core Web API
- .NET 8
- Entity Framework Core 8
- SQLite
- xUnit

## 3. Key Features

- Create property showing slots with input validation.
- Retrieve slots with filtering, sorting and pagination.
- Book slots for multiple attendees.
- Prevent overbooking through atomic database updates.
- Prevent duplicate active bookings using a unique filtered index.
- Allow owners and renters to cancel bookings before the showing starts.
- Allow owners to mark bookings as Attended or NoShow after the showing starts.
- Maintain booking history with status, actor, reason and timestamp.
- Handle validation errors, missing resources and booking conflicts using appropriate HTTP status codes.

## 4. Setup and Run

### Prerequisites
- .NET 8 SDK

### Step 1: Restore dependencies

```bash
dotnet restore
```

### Step 2: Apply database migrations

```bash
dotnet ef database update --project src/PropertyShowingBooking.Api
```

### Step 3: Run the application

```bash
dotnet run --project src/PropertyShowingBooking.Api
```

## 5. Running Tests

Execute the following command from the solution directory:

```bash
dotnet test
```

The test suite covers:

- Successful booking and seat reservation.
- Insufficient slot capacity.
- Duplicate active bookings.
- Booking cancellation and seat release.
- Booking status transition rules.
- Owner and renter authorization rules.
- Concurrent booking attempts.

## 6. Main Design Decisions

**Layered Architecture:** Separated API controllers, business logic, data access and domain entities to improve maintainability and testability.

**Entity Framework Core:** Used EF Core for database operations, entity relationships and transaction management.

**Concurrency Handling:** Used atomic conditional database updates to reserve seats and prevent overbooking.

**Data Integrity:** Implemented a unique filtered database index to prevent duplicate active bookings for the same renter and slot.

**Booking History:** Maintained a separate history entity to record every booking status transition for auditability.

**Business Rule Validation:** Centralized booking transition rules to ensure only permitted actors can perform valid status changes.

## 7. Future Improvements

Given more time, I would focus on:

- Implementing authentication and role-based authorization for owners and renters.
- Adding integration tests for concurrent booking requests and database transactions.
- Using PostgreSQL or SQL Server for production deployment.
- Implementing structured logging, monitoring and centralized exception handling.
- Improving API documentation with detailed Swagger examples.
- Setting up CI/CD pipelines for automated builds and tests.

## 8. AI Assistance

AI tools were used to assist with understanding requirements, exploring implementation approaches, debugging and developing test scenarios. Suggestions were reviewed, integrated and verified through application builds and automated tests.

---

**Project:** Property Showing Booking API  
**Framework:** .NET 8  
**Database:** SQLite  
**Testing Framework:** xUnit