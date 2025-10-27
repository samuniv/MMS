# Meeting Management System

A web-based application designed specifically for Nepali government offices to streamline meeting organization, scheduling, and documentation processes.

## Technology Stack

- **Backend**: ASP.NET Core 9.0
- **Frontend**: Razor Pages with DaisyUI and Tailwind CSS
- **Database**: PostgreSQL 16 with Entity Framework Core
- **Containerization**: Docker Compose
- **Authentication**: ASP.NET Core Identity
- **Logging**: Serilog

## Quick Start

### Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK (for local development)

### Running with Docker Compose

1. Clone the repository
2. Navigate to the project root directory
3. Start the services:

```bash
docker-compose up -d
```

This will start:
- PostgreSQL database on port 5432
- Meeting Management System web application on port 5000
- MailHog email testing service on port 8025 (web UI)

### Access the Application

- **Web Application**: http://localhost:5000
- **MailHog (Email Testing)**: http://localhost:8025
- **Database**: localhost:5432 (postgres/postgres)

### Development Setup

For local development without Docker:

1. Ensure PostgreSQL is running locally
2. Update connection string in `appsettings.Development.json`
3. Run the application:

```bash
cd src/MeetingManagementSystem.Web
dotnet run
```

### Database Migrations

To create and apply database migrations:

```bash
# Add migration
dotnet ef migrations add InitialCreate --project src/MeetingManagementSystem.Infrastructure --startup-project src/MeetingManagementSystem.Web

# Update database
dotnet ef database update --project src/MeetingManagementSystem.Infrastructure --startup-project src/MeetingManagementSystem.Web
```

## Project Structure

```
MeetingManagementSystem/
├── src/
│   ├── MeetingManagementSystem.Web/          # Razor Pages Application
│   ├── MeetingManagementSystem.Core/         # Business Logic & Entities
│   └── MeetingManagementSystem.Infrastructure/ # Data Access & External Services
├── docker-compose.yml                        # Docker Compose configuration
├── Dockerfile                               # Application container
└── README.md
```

## Configuration

Key configuration settings in `appsettings.json`:

- **ConnectionStrings**: Database connection
- **MeetingSettings**: Meeting-specific configurations
- **EmailSettings**: SMTP configuration for notifications

## Features

- Meeting scheduling and management
- Room booking and availability checking
- User authentication and role-based access
- Email notifications and reminders
- Document management
- Meeting minutes and action items
- Reporting and analytics

## Development

The application follows clean architecture principles with separation of concerns:

- **Web Layer**: Razor Pages, Controllers, and UI
- **Core Layer**: Business entities, enums, and interfaces
- **Infrastructure Layer**: Data access, external services, and implementations