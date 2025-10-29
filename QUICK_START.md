# Meeting Management System - Quick Start Guide

## Development Environment

### Prerequisites
- Docker Desktop
- .NET 9.0 SDK
- Git

### Quick Start

1. **Clone the repository**
```bash
git clone <repository-url>
cd MeetingManagementSystem
```

2. **Start development environment**
```bash
docker-compose up -d
```

3. **Access the application**
- Application: http://localhost:5000
- MailHog UI: http://localhost:8025
- PostgreSQL: localhost:5432

4. **Default credentials**
- Admin: admin@gov.np / Admin@123
- Official: official@gov.np / Official@123

## Production Deployment

### Prerequisites
- Docker Engine 20.10+
- Docker Compose 2.0+
- 4GB RAM minimum
- 20GB disk space

### Deployment Steps

1. **Configure environment**
```bash
cp .env.example .env
# Edit .env with your production values
```

2. **Build and deploy**
```bash
docker-compose -f docker-compose.prod.yml build
docker-compose -f docker-compose.prod.yml up -d
```

3. **Verify deployment**
```bash
# Check service health
docker-compose -f docker-compose.prod.yml ps

# Check application health
curl http://localhost:5000/health
```

4. **View logs**
```bash
docker-compose -f docker-compose.prod.yml logs -f app
```

## Staging Environment

### Deploy to staging
```bash
docker-compose -f docker-compose.staging.yml up -d
```

### Access staging
- Application: http://localhost:5001
- MailHog UI: http://localhost:8026

## Common Commands

### Development
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f

# Rebuild application
docker-compose up -d --build app

# Run migrations
docker-compose exec app dotnet ef database update
```

### Production
```bash
# Start services
docker-compose -f docker-compose.prod.yml up -d

# Stop services
docker-compose -f docker-compose.prod.yml down

# View logs
docker-compose -f docker-compose.prod.yml logs -f app

# Backup database
docker-compose -f docker-compose.prod.yml run --rm backup

# Monitor services
./scripts/monitor.sh
```

### Database Management
```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U postgres -d meetingmanagement

# Backup database
docker-compose exec postgres pg_dump -U postgres meetingmanagement > backup.sql

# Restore database
docker-compose exec -T postgres psql -U postgres meetingmanagement < backup.sql

# Run migrations
docker-compose exec app dotnet ef database update
```

## Testing

### Run all tests
```bash
dotnet test
```

### Run deployment tests
```bash
# Windows
.\scripts\run-deployment-tests.ps1

# Linux/Mac
./scripts/run-deployment-tests.sh
```

### Run specific test suite
```bash
dotnet test --filter "FullyQualifiedName~MeetingServiceTests"
```

## Troubleshooting

### Application won't start
```bash
# Check logs
docker-compose logs app

# Verify database connection
docker-compose exec app dotnet ef database update --verbose

# Restart services
docker-compose restart
```

### Database connection issues
```bash
# Check PostgreSQL status
docker-compose exec postgres pg_isready

# Verify connection string
docker-compose exec app env | grep ConnectionStrings

# Restart PostgreSQL
docker-compose restart postgres
```

### Port conflicts
```bash
# Check what's using the port
netstat -ano | findstr :5000  # Windows
lsof -i :5000                 # Linux/Mac

# Change port in docker-compose.yml or .env
APP_PORT=5001
```

## Monitoring

### Health Check
```bash
curl http://localhost:5000/health
```

### Container Stats
```bash
docker stats meetingmanagement_app meetingmanagement_postgres
```

### Application Logs
```bash
# Real-time logs
docker-compose logs -f app

# Last 100 lines
docker-compose logs --tail=100 app

# Logs since 1 hour ago
docker-compose logs --since 1h app
```

## Maintenance

### Update Application
```bash
git pull
docker-compose build app
docker-compose up -d app
```

### Clean Up
```bash
# Remove stopped containers
docker-compose down

# Remove volumes (WARNING: deletes data)
docker-compose down -v

# Clean Docker system
docker system prune -f
```

### Database Maintenance
```bash
# Vacuum database
docker-compose exec postgres psql -U postgres -d meetingmanagement -c "VACUUM ANALYZE;"

# Check database size
docker-compose exec postgres psql -U postgres -d meetingmanagement -c "SELECT pg_size_pretty(pg_database_size('meetingmanagement'));"
```

## Security Checklist

- [ ] Change default passwords in `.env`
- [ ] Configure SSL/TLS certificates
- [ ] Set up firewall rules
- [ ] Enable automated backups
- [ ] Configure log monitoring
- [ ] Update SMTP credentials
- [ ] Review and update allowed hosts
- [ ] Enable rate limiting
- [ ] Configure CORS policies
- [ ] Set up audit logging

## Support

For detailed documentation, see:
- [Deployment Guide](DEPLOYMENT.md)
- [Performance Optimization](docs/PERFORMANCE_OPTIMIZATION.md)
- [README](README.md)

For issues:
1. Check logs: `docker-compose logs`
2. Verify configuration: `docker-compose config`
3. Check health: `curl http://localhost:5000/health`
4. Review documentation
5. Contact system administrator
