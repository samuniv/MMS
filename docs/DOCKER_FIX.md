# Docker Compose Fix - Application Not Running

## Issue

The application container was not starting when running `docker-compose up`. Only the PostgreSQL and MailHog containers were running.

## Root Cause

The `docker-compose.override.yml` file was configured with `target: base` in the build section. This caused Docker to only build up to the `base` stage of the multi-stage Dockerfile, which only contains the ASP.NET runtime without the compiled application.

```yaml
# INCORRECT - docker-compose.override.yml
services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
      target: base  # ❌ This only builds the runtime, not the app
```

The `base` stage in the Dockerfile only sets up the runtime environment:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
```

## Solution

Changed the build target from `base` to `final` in `docker-compose.override.yml`:

```yaml
# CORRECT - docker-compose.override.yml
services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
      target: final  # ✅ This builds the complete application
```

Also removed the unnecessary volume mount that was trying to mount source code:

```yaml
# REMOVED
volumes:
  - ./src:/src  # Not needed for containerized deployment
```

## Dockerfile Stages Explained

The Dockerfile uses a multi-stage build with the following stages:

1. **base**: ASP.NET runtime only (for running the app)
2. **build**: .NET SDK + restored dependencies + built application
3. **publish**: Published application ready for deployment
4. **final**: Runtime + published application + configured directories

For development with Docker Compose, we need the **final** stage which includes the complete, compiled application.

## Verification

After the fix:

```bash
# Rebuild the application
docker-compose build app

# Start the application
docker-compose up -d app

# Verify it's running
docker-compose ps
```

Expected output:
```
NAME                       IMAGE       STATUS
meetingmanagement_app      mms-app     Up
meetingmanagement_postgres postgres:16 Up (healthy)
meetingmanagement_mailhog  mailhog     Up
```

Test the application:
```bash
curl http://localhost:5000
# Should return HTTP 200 OK
```

## Files Modified

- `docker-compose.override.yml`: Changed build target from `base` to `final`

## Additional Notes

### When to Use Each Stage

- **base**: Use for runtime-only scenarios (not applicable for this app)
- **build**: Intermediate stage for building (not used as target)
- **publish**: Intermediate stage for publishing (not used as target)
- **final**: Use for complete application deployment ✅

### Development vs Production

- **Development** (`docker-compose.yml` + `docker-compose.override.yml`):
  - Uses `final` stage
  - Mounts logs and uploads directories
  - Uses MailHog for email testing
  - Development environment variables

- **Production** (`docker-compose.prod.yml`):
  - Uses `final` stage explicitly
  - Includes health checks
  - Resource limits configured
  - Production environment variables
  - Backup service included

## Testing

The application is now accessible at:
- Application: http://localhost:5000
- MailHog UI: http://localhost:8025
- PostgreSQL: localhost:5432

Default credentials:
- Admin: admin@gov.np / Admin@123
- Official: official@gov.np / Official@123

## Logs

View application logs:
```bash
docker-compose logs -f app
```

The logs show successful startup:
```
[INF] Now listening on: http://[::]:5000
[INF] Application started. Press Ctrl+C to shut down.
[INF] Hosting environment: Development
```
