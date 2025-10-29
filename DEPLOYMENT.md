# Meeting Management System - Production Deployment Guide

## Prerequisites

- Docker Engine 20.10 or higher
- Docker Compose 2.0 or higher
- Minimum 4GB RAM
- Minimum 20GB disk space
- SSL certificate (for HTTPS)
- SMTP server credentials

## Initial Setup

### 1. Environment Configuration

Copy the example environment file and configure it:

```bash
cp .env.example .env
```

Edit `.env` and set the following required variables:

- `POSTGRES_PASSWORD`: Strong password for PostgreSQL
- `SMTP_SERVER`: Your SMTP server address
- `SMTP_USERNAME`: SMTP authentication username
- `SMTP_PASSWORD`: SMTP authentication password
- `JWT_SECRET`: Random 32+ character string
- `ENCRYPTION_KEY`: Random 32+ character string

### 2. SSL/TLS Configuration (Optional but Recommended)

For production, configure a reverse proxy (nginx/traefik) with SSL certificates:

```bash
# Example nginx configuration
server {
    listen 443 ssl http2;
    server_name meetings.gov.np;
    
    ssl_certificate /etc/ssl/certs/meetings.gov.np.crt;
    ssl_certificate_key /etc/ssl/private/meetings.gov.np.key;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 3. Build and Deploy

```bash
# Build the application image
docker-compose -f docker-compose.prod.yml build

# Start the services
docker-compose -f docker-compose.prod.yml up -d

# Check service status
docker-compose -f docker-compose.prod.yml ps

# View logs
docker-compose -f docker-compose.prod.yml logs -f app
```

### 4. Database Initialization

The database will be automatically initialized on first run. To verify:

```bash
# Check database connectivity
docker-compose -f docker-compose.prod.yml exec postgres psql -U postgres -d meetingmanagement -c "\dt"

# Run migrations manually if needed
docker-compose -f docker-compose.prod.yml exec app dotnet ef database update
```

## Backup and Recovery

### Automated Backups

Configure automated backups using cron:

```bash
# Add to crontab (runs daily at 2 AM)
0 2 * * * cd /path/to/app && docker-compose -f docker-compose.prod.yml run --rm backup
```

### Manual Backup

```bash
# Create a backup
docker-compose -f docker-compose.prod.yml run --rm backup

# List available backups
docker-compose -f docker-compose.prod.yml exec postgres ls -lh /backups
```

### Restore from Backup

```bash
# Copy restore script to container
docker cp scripts/restore.sh meetingmanagement_postgres_prod:/restore.sh

# Run restore
docker-compose -f docker-compose.prod.yml exec postgres sh /restore.sh /backups/meetingmanagement_backup_YYYYMMDD_HHMMSS.sql.gz
```

## Monitoring and Health Checks

### Health Check Endpoint

The application exposes a health check endpoint at `/health`:

```bash
# Check application health
curl http://localhost:5000/health

# Expected response: "Healthy"
```

### Container Health Status

```bash
# Check container health
docker-compose -f docker-compose.prod.yml ps

# View health check logs
docker inspect --format='{{json .State.Health}}' meetingmanagement_app_prod | jq
```

### Log Monitoring

```bash
# View application logs
docker-compose -f docker-compose.prod.yml logs -f app

# View PostgreSQL logs
docker-compose -f docker-compose.prod.yml logs -f postgres

# View logs from specific time
docker-compose -f docker-compose.prod.yml logs --since 1h app
```

### Resource Monitoring

```bash
# Monitor resource usage
docker stats meetingmanagement_app_prod meetingmanagement_postgres_prod

# Check disk usage
docker system df
```

## Maintenance

### Update Application

```bash
# Pull latest changes
git pull

# Rebuild and restart
docker-compose -f docker-compose.prod.yml build app
docker-compose -f docker-compose.prod.yml up -d app

# Verify deployment
curl http://localhost:5000/health
```

### Database Maintenance

```bash
# Vacuum database
docker-compose -f docker-compose.prod.yml exec postgres psql -U postgres -d meetingmanagement -c "VACUUM ANALYZE;"

# Check database size
docker-compose -f docker-compose.prod.yml exec postgres psql -U postgres -d meetingmanagement -c "SELECT pg_size_pretty(pg_database_size('meetingmanagement'));"

# Reindex database
docker-compose -f docker-compose.prod.yml exec postgres psql -U postgres -d meetingmanagement -c "REINDEX DATABASE meetingmanagement;"
```

### Log Rotation

Logs are automatically rotated by Docker. To manually clean old logs:

```bash
# Clean Docker logs
docker-compose -f docker-compose.prod.yml exec app find /app/logs -name "*.log" -mtime +30 -delete

# Clean Docker system logs
docker system prune -f
```

## Scaling

### Horizontal Scaling

To run multiple application instances:

```bash
# Scale to 3 instances
docker-compose -f docker-compose.prod.yml up -d --scale app=3

# Use a load balancer (nginx/haproxy) to distribute traffic
```

### Vertical Scaling

Adjust resource limits in `docker-compose.prod.yml`:

```yaml
deploy:
  resources:
    limits:
      cpus: '4'
      memory: 4G
    reservations:
      cpus: '2'
      memory: 2G
```

## Troubleshooting

### Application Won't Start

```bash
# Check logs
docker-compose -f docker-compose.prod.yml logs app

# Check environment variables
docker-compose -f docker-compose.prod.yml exec app env

# Verify database connection
docker-compose -f docker-compose.prod.yml exec app dotnet ef database update --verbose
```

### Database Connection Issues

```bash
# Check PostgreSQL status
docker-compose -f docker-compose.prod.yml exec postgres pg_isready

# Check connection from app container
docker-compose -f docker-compose.prod.yml exec app ping postgres

# Verify credentials
docker-compose -f docker-compose.prod.yml exec postgres psql -U postgres -d meetingmanagement -c "SELECT version();"
```

### Performance Issues

```bash
# Check resource usage
docker stats

# Analyze slow queries
docker-compose -f docker-compose.prod.yml exec postgres psql -U postgres -d meetingmanagement -c "SELECT * FROM pg_stat_statements ORDER BY total_exec_time DESC LIMIT 10;"

# Check connection pool
docker-compose -f docker-compose.prod.yml exec postgres psql -U postgres -d meetingmanagement -c "SELECT * FROM pg_stat_activity;"
```

## Security Checklist

- [ ] Change default passwords in `.env`
- [ ] Configure SSL/TLS certificates
- [ ] Set up firewall rules
- [ ] Enable automated backups
- [ ] Configure log monitoring
- [ ] Set up intrusion detection
- [ ] Regular security updates
- [ ] Implement rate limiting
- [ ] Configure CORS policies
- [ ] Enable audit logging

## Support

For issues and support:
- Check logs: `docker-compose -f docker-compose.prod.yml logs`
- Review health status: `curl http://localhost:5000/health`
- Contact system administrator
