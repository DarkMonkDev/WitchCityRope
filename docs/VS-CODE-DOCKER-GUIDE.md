# VS Code with Docker - Quick Guide

## The Right Way to Work with Docker

### 1. Start Docker Containers (if not already running)
```bash
docker-compose up -d
```

### 2. Access the Application
- **Web UI**: http://localhost:5651
- **API**: http://localhost:5653
- **Do NOT use**: http://localhost:8080 (this is internal to Docker)

### 3. Using VS Code

#### Option A: Press F5 (After Setup)
Now when you press F5, it should open Chrome to http://localhost:5651

#### Option B: Use Terminal
```bash
./open-docker-site.sh
```

#### Option C: Use VS Code Tasks
1. Press `Ctrl+Shift+B` (Run Build Task)
2. Select "Open Docker Site"

#### Option D: Manual
Just open http://localhost:5651 in your browser

### 4. View Logs (to see hot reload working)
- Terminal: `docker-compose logs -f web`
- Or VS Code: `Ctrl+Shift+P` → "Tasks: Run Task" → "docker-logs-web"

## If F5 Still Opens Wrong URL

1. **Close VS Code completely**
2. **Clear any VS Code cache**:
   ```bash
   rm -rf ~/.config/Code/Cache/*
   rm -rf ~/.config/Code/CachedData/*
   ```
3. **Reopen VS Code**
4. **Try F5 again**

## Important Points

- The application runs INSIDE Docker containers
- Port 8080 is INTERNAL to the containers
- You access it via port 5651 (web) and 5653 (API)
- Code changes are automatically detected (hot reload)
- No need to rebuild containers for code changes

## Quick Commands

```bash
# Check if containers are running
docker-compose ps

# View logs
docker-compose logs -f

# Restart containers
docker-compose restart

# Stop everything
docker-compose down

# Start everything
docker-compose up -d
```