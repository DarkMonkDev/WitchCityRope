# WitchCityRope Deployment Guide

**IMPORTANT**: This project now uses **GitHub Actions CI/CD** for automated deployments to DigitalOcean.

## 🚀 Quick Start - GitHub Actions CI/CD (Recommended)

### New Automated Deployment Method
For DigitalOcean deployment with GitHub Actions:
👉 **See [GITHUB-ACTIONS-SETUP.md](./GITHUB-ACTIONS-SETUP.md)** for the complete step-by-step guide.

**Quick Summary**:
1. Configure GitHub Secrets (9 secrets required)
2. Push to `staging` branch → Automatic deployment to https://staging.notfai.com
3. Trigger production deployment → Manual approval → Deploy to https://notfai.com

### Legacy Deployment Methods
This directory also contains legacy deployment scripts for manual deployments and other hosting providers.

## 📁 Directory Structure

```
deployment/
├── README.md                           # This file
├── deployment-config.json              # Main deployment configuration
├── deploy-windows.ps1                  # PowerShell script for Windows deployment
├── deploy-linux.sh                     # Bash script for Linux deployment
├── docker-deploy.yml                   # Docker production configuration
├── environment-setup-checklist.md      # Pre-deployment checklist
├── pre-deployment-validation.sh        # Validation script
├── post-deployment-health-check.sh     # Health check script
├── rollback-procedures.md              # Rollback documentation
└── configs/                            # Environment-specific configs
    ├── staging/
    │   └── .env.example
    └── production/
        └── .env.example
```

## 🚀 Quick Start

### For Docker Deployment (Recommended)

```bash
# 1. Run pre-deployment validation
./pre-deployment-validation.sh

# 2. Deploy to staging
./deploy-linux.sh -e staging

# 3. Run health checks
./post-deployment-health-check.sh

# 4. Deploy to production
./deploy-linux.sh -e production
```

### For Windows VPS

```powershell
# 1. Run deployment
.\deploy-windows.ps1 -Environment Production -DeploymentPath "C:\inetpub\WitchCityRope"

# 2. Verify deployment
.\post-deployment-health-check.ps1
```

## 📋 Deployment Options

### Linux/Docker Deployment

The `deploy-linux.sh` script supports the following options:

- `-e, --environment`: Environment to deploy (staging/production) **[Required]**
- `-p, --path`: Deployment path (default: `/opt/witchcityrope`)
- `-b, --backup-path`: Backup location (default: `/var/backups/witchcityrope`)
- `-c, --config`: Configuration file (default: `./deployment-config.json`)
- `--skip-backup`: Skip backup creation
- `--skip-health-check`: Skip post-deployment health checks
- `--force`: Force deployment even if checks fail

Example:
```bash
./deploy-linux.sh -e production -p /home/deploy/witchcityrope --backup-path /backups
```

### Windows Deployment

The `deploy-windows.ps1` script parameters:

- `-Environment`: Target environment **[Required]**
- `-DeploymentPath`: Installation directory **[Required]**
- `-BackupPath`: Backup directory (default: `C:\Backups\WitchCityRope`)
- `-ConfigPath`: Config file path (default: `.\deployment-config.json`)
- `-SkipBackup`: Skip backup creation
- `-SkipHealthCheck`: Skip health verification
- `-Force`: Force deployment

Example:
```powershell
.\deploy-windows.ps1 -Environment Production `
                     -DeploymentPath "C:\inetpub\WitchCityRope" `
                     -BackupPath "D:\Backups"
```

## 🔧 Configuration

### deployment-config.json

The main configuration file contains:

- **Application settings**: Ports, paths, versions
- **Environment configs**: Domain, SSL, logging levels
- **Docker settings**: Registry, volumes, networks
- **Security settings**: HTTPS, HSTS, rate limiting
- **Monitoring**: Prometheus, Grafana, health checks
- **Backup settings**: Schedule, retention, location

### Environment Variables

Create `.env` file for each environment:

```bash
# Required variables
JWT_SECRET=your-secret-key-min-32-chars
SENDGRID_API_KEY=your-sendgrid-key
PAYPAL_CLIENT_ID=your-paypal-client-id
PAYPAL_CLIENT_SECRET=your-paypal-secret
ENCRYPTION_KEY=your-32-char-encryption-key

# Optional
REDIS_PASSWORD=your-redis-password
GRAFANA_PASSWORD=your-grafana-admin-password
```

### Backup Configuration

The application uses environment-specific backup storage in DigitalOcean Spaces:

```
witchcityrope (bucket)
├── backups/
│   ├── local/              # Development environment
│   ├── staging/            # Staging environment
│   └── production/         # Production environment
```

**Environment Variables Required**:
```bash
DIGITALOCEAN_SPACES_ACCESS_KEY=<your-access-key>
DIGITALOCEAN_SPACES_SECRET_KEY=<your-secret-key>
```

**Configuration** (set in docker-compose files):
- Local: `BackupConfiguration__Spaces__FolderPrefix=backups/local`
- Staging: `BackupConfiguration__Spaces__FolderPrefix=backups/staging`
- Production: `BackupConfiguration__Spaces__FolderPrefix=backups/production`

Each environment can only access its own backups, preventing accidental cross-environment restoration.

For details, see [Database Backup & Restore Documentation](../docs/functional-areas/database-backup-restore/README.md).

## 🐳 Docker Deployment

### Using Docker Compose

1. **Build images**:
   ```bash
   docker-compose -f docker-compose.yml -f docker-deploy.yml build
   ```

2. **Deploy**:
   ```bash
   docker-compose -f docker-compose.yml -f docker-deploy.yml up -d
   ```

3. **Scale services**:
   ```bash
   docker-compose -f docker-compose.yml -f docker-deploy.yml up -d --scale api=3
   ```

### Docker Swarm Mode

For high availability:

```bash
# Initialize swarm
docker swarm init

# Deploy stack
docker stack deploy -c docker-deploy.yml witchcityrope

# Scale service
docker service scale witchcityrope_api=3
```

## 🔍 Health Checks

### Automated Health Checks

Run the health check script:
```bash
./post-deployment-health-check.sh
```

Options:
- `-a, --api-url`: API URL (default: http://localhost:5653)
- `-w, --web-url`: Web URL (default: http://localhost:5651)
- `-t, --timeout`: Request timeout in seconds
- `-r, --retries`: Number of retries

### Manual Health Checks

1. **API Health**: `curl http://localhost:5653/health`
2. **Web Health**: `curl http://localhost:5651/health`
3. **Database**: Check `/api/health/db`
4. **External Services**: Verify email and payment integration

## 🔄 Rollback Procedures

If deployment fails, follow the rollback procedures:

1. **Quick rollback**:
   ```bash
   cd /opt/witchcityrope
   ./deployment/rollback.sh
   ```

2. **Manual rollback**: See [rollback-procedures.md](rollback-procedures.md)

## 🛡️ Security Considerations

1. **SSL/TLS**:
   - Use Let's Encrypt for free SSL certificates
   - Enable HSTS with minimum 1-year duration
   - Use TLS 1.2+ only

2. **Secrets Management**:
   - Never commit secrets to git
   - Use environment variables
   - Rotate keys regularly

3. **Firewall Rules**:
   - Only open required ports (80, 443)
   - Restrict SSH/RDP access
   - Enable rate limiting

## 📊 Monitoring

### Prometheus + Grafana

Access monitoring dashboards:
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000

Default Grafana credentials:
- Username: admin
- Password: (set in GRAFANA_PASSWORD env var)

### Log Aggregation

Logs are collected by Promtail and sent to Loki:
- API logs: `/app/logs/api/`
- Web logs: `/app/logs/web/`
- Nginx logs: `/var/log/nginx/`

## 🚨 Troubleshooting

### Common Issues

1. **Port already in use**:
   ```bash
   # Find process using port
   sudo lsof -i :5653
   # Kill process
   sudo kill -9 <PID>
   ```

2. **Docker permission denied**:
   ```bash
   # Add user to docker group
   sudo usermod -aG docker $USER
   # Logout and login again
   ```

3. **Database locked**:
   ```bash
   # Check database processes
   fuser /opt/witchcityrope/data/witchcityrope.db
   ```

4. **SSL certificate issues**:
   ```bash
   # Test certificate
   openssl s_client -connect localhost:443 -servername witchcityrope.com
   ```

### Debug Mode

Enable debug logging:
```bash
# Set in .env
LOG_LEVEL=Debug
ASPNETCORE_ENVIRONMENT=Development
```

## 📝 Maintenance

### Regular Tasks

1. **Daily**:
   - Monitor health endpoints
   - Check error logs
   - Verify backups

2. **Weekly**:
   - Review performance metrics
   - Update dependencies
   - Check disk space

3. **Monthly**:
   - Rotate logs
   - Update SSL certificates
   - Security patches

### Database Maintenance

```bash
# Vacuum database (SQLite)
sqlite3 /opt/witchcityrope/data/witchcityrope.db "VACUUM;"

# Check integrity
sqlite3 /opt/witchcityrope/data/witchcityrope.db "PRAGMA integrity_check;"
```

## 🤝 Support

For deployment issues:

1. Check logs in `/app/logs/`
2. Run health check script
3. Review [troubleshooting guide](#-troubleshooting)
4. Contact DevOps team

## 📚 Additional Resources

- [Environment Setup Checklist](environment-setup-checklist.md)
- [Rollback Procedures](rollback-procedures.md)
- [Docker Documentation](https://docs.docker.com/)
- [Nginx Documentation](https://nginx.org/en/docs/)
- [Let's Encrypt Guide](https://letsencrypt.org/getting-started/)
