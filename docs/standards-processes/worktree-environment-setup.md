# Worktree Environment Setup Guide

## Overview

This guide defines the MANDATORY environment setup procedures for git worktrees in the WitchCityRope project. Every worktree must be properly configured before any development work begins.

## Environment File Synchronization Procedures

### Required Environment Files

Each worktree MUST have these files copied from the main repository:

1. **`.env` - Application environment variables**
   ```bash
   cp /home/chad/repos/witchcityrope-react/.env .
   ```

2. **`.env.local` - Local development overrides (if exists)**
   ```bash
   cp /home/chad/repos/witchcityrope-react/.env.local . 2>/dev/null || true
   ```

3. **`.env.development` - Development-specific settings (if exists)**
   ```bash
   cp /home/chad/repos/witchcityrope-react/.env.development . 2>/dev/null || true
   ```

### Environment Validation Checklist

After copying environment files, verify:
- [ ] Database connection strings are correct
- [ ] API endpoints point to correct services
- [ ] Authentication keys are present
- [ ] Port configurations don't conflict

## Docker Configuration for Parallel Development

### Port Management Strategy

Each worktree should use different ports to avoid conflicts:

#### Worktree Port Assignments
```yaml
# Main Repository (not for development)
- Web: 5173
- API: 5653

# Worktree 1
- Web: 5174
- API: 5654

# Worktree 2
- Web: 5175
- API: 5655

# Worktree 3
- Web: 5176
- API: 5656
```

### Docker Compose Override

Create `docker-compose.override.yml` in each worktree:

```yaml
version: '3.8'
services:
  web:
    ports:
      - "5174:5173"  # Map to different external port
    environment:
      - VITE_API_URL=http://localhost:5654
  
  api:
    ports:
      - "5654:5653"  # Map to different external port
```

### Database Isolation

Options for database isolation:

1. **Shared Database (Recommended for most cases)**
   - All worktrees use same PostgreSQL instance
   - Different schemas or prefixed tables if needed

2. **Separate Databases (For conflicting migrations)**
   ```bash
   # In worktree .env
   DATABASE_URL=postgresql://user:pass@localhost:5433/witchcity_feature_auth
   ```

## Node.js Dependency Management

### Initial Setup

```bash
# In worktree directory
npm install

# Verify installation
npm list --depth=0
```

### Dependency Synchronization

When main repository updates dependencies:

```bash
# In worktree
git pull origin main  # Get package.json updates
npm install          # Update dependencies
```

### Lock File Handling

- **Always commit** `package-lock.json` changes
- **Never ignore** lock file conflicts
- **Resolve properly** during merges

## Database Connection Configuration

### PostgreSQL Connection String Format

```bash
# Standard format
DATABASE_URL=postgresql://postgres:WitchCity2024!@localhost:5433/witchcityrope_dev

# With SSL (production)
DATABASE_URL=postgresql://user:pass@host:5433/db?sslmode=require
```

### Connection Pooling

For parallel development, adjust pool settings:

```javascript
// In database configuration
{
  max: 5,  // Reduce pool size for multiple instances
  min: 2,
  idle: 10000
}
```

## Complete Setup Script

### Automated Worktree Setup

Create this as `/scripts/worktree/setup-worktree-environment.sh`:

```bash
#!/bin/bash
# Automated worktree environment setup

WORKTREE_PATH=$1
MAIN_REPO="/home/chad/repos/witchcityrope-react"

if [ -z "$WORKTREE_PATH" ]; then
  echo "Usage: ./setup-worktree-environment.sh <worktree-path>"
  exit 1
fi

echo "Setting up environment for $WORKTREE_PATH..."

# Copy environment files
cp "$MAIN_REPO/.env" "$WORKTREE_PATH/" 2>/dev/null || echo "Warning: .env not found"
cp "$MAIN_REPO/.env.local" "$WORKTREE_PATH/" 2>/dev/null || true
cp "$MAIN_REPO/.env.development" "$WORKTREE_PATH/" 2>/dev/null || true

# Install dependencies
cd "$WORKTREE_PATH"
npm install

# Create port configuration
PORT_OFFSET=$(git worktree list | grep -n "$WORKTREE_PATH" | cut -d: -f1)
WEB_PORT=$((5173 + PORT_OFFSET))
API_PORT=$((5653 + PORT_OFFSET))

echo "PORT=$WEB_PORT" >> .env.local
echo "API_PORT=$API_PORT" >> .env.local

echo "Environment setup complete!"
echo "Web port: $WEB_PORT"
echo "API port: $API_PORT"
```

## Verification Procedures

### Environment Health Check

```bash
#!/bin/bash
# Check worktree environment health

echo "Checking environment setup..."

# Check environment files
[ -f .env ] && echo "✅ .env exists" || echo "❌ .env missing"
[ -f node_modules/.bin/vite ] && echo "✅ Dependencies installed" || echo "❌ Dependencies missing"

# Check database connection
npm run db:test 2>/dev/null && echo "✅ Database connected" || echo "❌ Database connection failed"

# Check port availability
lsof -i:5173 > /dev/null 2>&1 && echo "⚠️  Default port in use" || echo "✅ Port available"
```

## Common Environment Issues

### Issue: Missing Environment Variables
```bash
# Solution: Re-copy from main repository
cp /home/chad/repos/witchcityrope-react/.env .
```

### Issue: Port Conflicts
```bash
# Solution: Use different port
PORT=5175 npm run dev
```

### Issue: Database Connection Refused
```bash
# Solution: Check PostgreSQL is running
sudo systemctl status postgresql
# Or with Docker
docker ps | grep postgres
```

### Issue: Dependency Version Conflicts
```bash
# Solution: Clean install
rm -rf node_modules package-lock.json
npm install
```

## Environment Sync Automation

### Git Hook for Environment Updates

Create `.git/hooks/post-checkout` in worktree:

```bash
#!/bin/bash
# Auto-sync environment on branch switch

MAIN_REPO="/home/chad/repos/witchcityrope-react"

# Check if .env is older than main
if [ "$MAIN_REPO/.env" -nt ".env" ]; then
  echo "Updating .env from main repository..."
  cp "$MAIN_REPO/.env" .
fi
```

## Security Considerations

### Never Commit Environment Files
Ensure `.gitignore` includes:
```
.env
.env.local
.env.*.local
```

### Sensitive Data Handling
- Use environment variables for all secrets
- Never hardcode credentials
- Rotate keys regularly

## Monitoring and Maintenance

### Active Worktree Monitoring
```bash
# Check all worktrees and their status
for wt in $(git worktree list --porcelain | grep "worktree" | cut -d' ' -f2); do
  echo "Worktree: $wt"
  [ -f "$wt/.env" ] && echo "  ✅ Environment configured" || echo "  ❌ Environment missing"
  [ -d "$wt/node_modules" ] && echo "  ✅ Dependencies installed" || echo "  ❌ Dependencies missing"
done
```

### Cleanup Validation
Before removing a worktree, ensure:
- [ ] No uncommitted environment changes
- [ ] No running services
- [ ] No open database connections

---
*This guide ensures consistent environment setup across all worktrees*
*Last Updated: 2025-08-23*