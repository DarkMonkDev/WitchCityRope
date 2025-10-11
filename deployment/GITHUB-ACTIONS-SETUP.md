# GitHub Actions CI/CD Setup Guide
**Project**: WitchCityRope
**Date**: October 10, 2025
**Infrastructure**: DigitalOcean (Droplet + Managed PostgreSQL)

## Overview

This guide walks you through setting up automated CI/CD deployment using GitHub Actions for the WitchCityRope application.

---

## Prerequisites

Before starting, ensure you have:
- ✅ DigitalOcean droplet running (witchcityrope-prod: 104.131.165.14)
- ✅ DigitalOcean managed PostgreSQL database
- ✅ DigitalOcean Container Registry created
- ✅ SSH access configured on the server (witchcity user)
- ✅ DNS configured (notfai.com, staging.notfai.com)
- ✅ SSL certificates installed
- ✅ Nginx reverse proxy configured
- ✅ GitHub repository access

---

## Step 1: Retrieve SSH Private Key from Server

The SSH private key needed for GitHub Actions is already created on the server at `/home/witchcity/.ssh/witchcity-deploy`.

### Get the Private Key

SSH into the server and copy the private key:

```bash
# SSH into the server
ssh witchcity@104.131.165.14

# Display the private key
cat /home/witchcity/.ssh/witchcity-deploy

# Copy the entire output (including -----BEGIN and -----END lines)
```

**Expected Output**:
```
-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAAAEbm9uZQAAAAAAAAABAAAAMwAAAAtz
...
[many lines of base64 encoded key data]
...
-----END OPENSSH PRIVATE KEY-----
```

**IMPORTANT**: Copy this ENTIRE output exactly as shown, including the header and footer lines.

---

## Step 2: Get Database Connection Strings

The database connection strings are stored on the server in `/opt/witchcityrope/database-credentials.env`.

```bash
# On the server
cat /opt/witchcityrope/database-credentials.env
```

You'll need:
- `PROD_CONNECTION_STRING` - for production deployments
- `STAGING_CONNECTION_STRING` - for staging deployments

---

## Step 3: Generate JWT Secret

Generate a secure random string for JWT token signing:

```bash
# On your local machine or the server
openssl rand -base64 64
```

**Example Output**:
```
xK8vR3mN2pQ7wL9sT4hU6jF1cZ5yV0oB8eD3nA7iG2kX1mP4sW9qR6tY5uH8jL0c
```

Save this value - you'll use it for both staging and production.

---

## Step 4: Configure GitHub Secrets

Go to your GitHub repository:
1. Navigate to: **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Add each of the following secrets:

### Required Secrets

| Secret Name | Value | Where to Get It |
|-------------|-------|-----------------|
| `DIGITALOCEAN_TOKEN` | `dop_v1_a830e104efd3e604de2815b7c2810dbaa5488a451b2a36344aa808d03b6ec427` | Already provided |
| `SSH_PRIVATE_KEY` | Contents from Step 1 | Server: `/home/witchcity/.ssh/witchcity-deploy` |
| `PRODUCTION_SERVER_HOST` | `104.131.165.14` | Droplet IP address |
| `PRODUCTION_SERVER_USER` | `witchcity` | Deploy user on server |
| `STAGING_SERVER_HOST` | `104.131.165.14` | Same server (shared droplet) |
| `STAGING_SERVER_USER` | `witchcity` | Same user |
| `PROD_DB_CONNECTION_STRING` | From Step 2 | Server: `/opt/witchcityrope/database-credentials.env` |
| `STAGING_DB_CONNECTION_STRING` | From Step 2 | Server: `/opt/witchcityrope/database-credentials.env` |
| `JWT_SECRET` | From Step 3 | Generated with openssl |

### Adding Each Secret

For each secret:
1. Click **New repository secret**
2. Enter the **Name** (exactly as shown in table above)
3. Paste the **Value**
4. Click **Add secret**

### Special Note: SSH_PRIVATE_KEY Format

When pasting the SSH private key:
- ✅ Include the `-----BEGIN OPENSSH PRIVATE KEY-----` line
- ✅ Include all the base64 content
- ✅ Include the `-----END OPENSSH PRIVATE KEY-----` line
- ✅ Make sure there are NO extra spaces or line breaks
- ✅ The key should be exactly as it appears in the file

**Correct Format**:
```
-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAAAEbm9uZQAAAAAAAAABAAAAMwAAAAtz
c2gtZWQyNTUxOQAAACBTMz/LrNHQPl cIUGsF30L74O0KKW1YJ7krUFghMiHmGQAA
...
-----END OPENSSH PRIVATE KEY-----
```

---

## Step 5: Verify Secrets Configuration

After adding all secrets, verify in GitHub:
1. Go to **Settings** → **Secrets and variables** → **Actions**
2. You should see all 9 secrets listed:
   - DIGITALOCEAN_TOKEN
   - SSH_PRIVATE_KEY
   - PRODUCTION_SERVER_HOST
   - PRODUCTION_SERVER_USER
   - STAGING_SERVER_HOST
   - STAGING_SERVER_USER
   - PROD_DB_CONNECTION_STRING
   - STAGING_DB_CONNECTION_STRING
   - JWT_SECRET

---

## Step 6: Commit and Push GitHub Actions Workflows

The GitHub Actions workflows are already created in `.github/workflows/`:
- `build-and-push.yml` - Builds Docker images and pushes to registry
- `deploy-staging.yml` - Deploys to staging environment
- `deploy-production.yml` - Deploys to production (with manual approval)

Commit and push these files:

```bash
# On your local machine
cd /home/chad/repos/witchcityrope-react

git add .github/workflows/
git add deployment/
git commit -m "feat: add GitHub Actions CI/CD workflows and deployment configs"
git push origin main
```

---

## Step 7: Create Staging Branch

Create a staging branch for automatic staging deployments:

```bash
git checkout -b staging
git push origin staging
```

---

## Step 8: Test the Deployment Pipeline

### Automatic Staging Deployment

1. Push a commit to the `staging` branch:
   ```bash
   git checkout staging
   # Make a change
   git commit -m "test: trigger staging deployment"
   git push origin staging
   ```

2. Watch the deployment:
   - Go to GitHub → **Actions** tab
   - You should see two workflows running:
     - "Build and Push Docker Images"
     - "Deploy to Staging"

3. Verify staging deployment:
   - Wait for workflows to complete (5-10 minutes)
   - Visit: https://staging.notfai.com
   - Check health: https://staging.notfai.com/api/health

### Manual Production Deployment

1. After staging is verified, deploy to production:
   - Go to GitHub → **Actions** tab
   - Click **Deploy to Production** workflow
   - Click **Run workflow**
   - Enter image tag: `latest`
   - Enter confirmation: `DEPLOY`
   - Click **Run workflow**

2. Watch the deployment:
   - The workflow will create a backup first
   - Then deploy to production
   - Run health checks

3. Verify production deployment:
   - Visit: https://notfai.com
   - Check health: https://notfai.com/api/health

---

## Deployment Workflow Summary

### Automatic Process (Staging)

```
Developer pushes to `staging` branch
    ↓
GitHub Actions: Build and Push
    ├─ Build API Docker image
    ├─ Build Web Docker image
    └─ Push to DigitalOcean Registry
    ↓
GitHub Actions: Deploy to Staging (Automatic)
    ├─ SSH into server
    ├─ Pull latest images
    ├─ Update containers
    ├─ Run migrations
    └─ Health checks
    ↓
Staging live at: https://staging.notfai.com
```

### Manual Process (Production)

```
Developer pushes to `main` branch
    ↓
GitHub Actions: Build and Push
    ├─ Build API Docker image
    ├─ Build Web Docker image
    └─ Push to DigitalOcean Registry
    ↓
Images ready for production deployment
    ↓
Developer manually triggers deployment
    ├─ Type "DEPLOY" to confirm
    ↓
GitHub Actions: Deploy to Production (Manual)
    ├─ Create backup
    ├─ SSH into server
    ├─ Pull latest images
    ├─ Update containers
    ├─ Run migrations
    └─ Health checks
    ↓
Production live at: https://notfai.com
```

---

## Troubleshooting

### SSH Connection Fails

**Error**: `Permission denied (publickey)`

**Solution**:
1. Verify SSH_PRIVATE_KEY secret is correct
2. Check the key format (must include BEGIN/END lines)
3. Verify server user is `witchcity`
4. Test SSH locally: `ssh -i ~/.ssh/witchcity-deploy witchcity@104.131.165.14`

### Docker Login Fails

**Error**: `Error response from daemon: Get https://registry.digitalocean.com/v2/: unauthorized`

**Solution**:
1. Verify DIGITALOCEAN_TOKEN secret is correct
2. Check token has registry access permissions
3. Verify registry exists: `witchcityrope`

### Container Health Checks Fail

**Error**: `Health check failed`

**Solution**:
1. SSH into server: `ssh witchcity@104.131.165.14`
2. Check container logs:
   ```bash
   cd /opt/witchcityrope/production
   docker-compose -f docker-compose.production.yml logs api
   docker-compose -f docker-compose.production.yml logs web
   ```
3. Check database connectivity
4. Verify environment variables in `.env.production`

### Database Migration Fails

**Error**: `Migration failed`

**Solution**:
1. Check database connection string
2. Verify database exists: `witchcityrope_production` or `witchcityrope_staging`
3. Test database connection manually:
   ```bash
   docker-compose -f docker-compose.production.yml exec api \
     dotnet ef database update --verbose
   ```

---

## Security Best Practices

1. **Rotate Secrets Regularly**
   - JWT_SECRET: Every 90 days
   - SSH keys: Annually or if compromised
   - Database passwords: Quarterly

2. **Limit Secret Access**
   - Only grant repository admin access to those who need it
   - Use GitHub's secret protection rules

3. **Monitor Deployments**
   - Review GitHub Actions logs regularly
   - Set up notifications for failed deployments
   - Monitor server logs for unauthorized access

4. **Backup Before Production**
   - Automated backup is created before each production deployment
   - Manual backups recommended before major changes
   - Test restore procedures quarterly

---

## Next Steps

After successful deployment:

1. **Set up monitoring** (Day 3 of deployment plan)
   - Configure health check monitoring
   - Set up log aggregation
   - Create alerting rules

2. **Configure automated backups** (Day 3 of deployment plan)
   - Database backups (daily)
   - Container volume backups (weekly)
   - Configuration backups (on change)

3. **Test disaster recovery**
   - Practice rollback procedures
   - Test backup restoration
   - Document recovery time objectives

4. **Optimize performance**
   - Monitor resource usage
   - Adjust container resource limits
   - Implement caching strategies

---

## Support and Documentation

- **Deployment Plan**: `/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/`
- **Implementation Checklist**: `/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/handoffs/IMPLEMENTATION-CHECKLIST.md`
- **Current Status**: `/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/CURRENT-STATUS-2025-10-10.md`
- **Server Access**: SSH to `witchcity@104.131.165.14`
- **DigitalOcean Console**: https://cloud.digitalocean.com

---

**Document Created**: 2025-10-10
**Last Updated**: 2025-10-10
**Status**: Ready for Implementation
