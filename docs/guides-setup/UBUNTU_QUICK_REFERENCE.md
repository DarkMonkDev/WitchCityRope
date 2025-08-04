# Ubuntu Development Environment - Quick Reference

## Essential Commands

### Start WitchCityRope Application
```bash
cd /home/chad/repos/witchcityrope/WitchCityRope
~/.dotnet/dotnet run --project src/WitchCityRope.Web
# Access at: http://localhost:5000
```

### Browser Automation

#### Browser-tools MCP (Precise Control)
```bash
cd /home/chad/mcp-servers/browser-tools-server
./start-server.sh
# Choose: 2 (Headed + Safe) for debugging
```

#### Stagehand MCP (Natural Language)
```bash
export OPENAI_API_KEY='your-api-key'
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh
```

### Database Operations
```bash
# Start PostgreSQL
sudo systemctl start postgresql

# Connect to PostgreSQL
sudo -u postgres psql

# Create database
sudo -u postgres createdb witchcityrope_db

# Set password (from psql)
ALTER USER postgres PASSWORD 'WitchCity2024!';
```

### Docker Operations
```bash
# Start Docker
sudo systemctl start docker

# Build and run with Docker
cd /home/chad/repos/witchcityrope/WitchCityRope
docker build -t witchcityrope .
docker run -p 5000:8080 witchcityrope

# Note: After adding user to docker group, logout/login required
```

### Git Operations
```bash
# Already configured with credentials
cd /home/chad/repos/witchcityrope/WitchCityRope
git pull
git add .
git commit -m "Your message"
git push

# GitHub CLI
gh repo view
gh issue list
gh pr create
```

### Testing
```bash
# Run all tests
cd /home/chad/repos/witchcityrope/WitchCityRope
~/.dotnet/dotnet test

# Run browser automation test
cd /home/chad/repos/witchcityrope
node test-browser-automation.js

# Test tools
/home/chad/repos/witchcityrope/test-all-tools.sh
```

## File Locations

### Project Files
- Main project: `/home/chad/repos/witchcityrope/WitchCityRope/`
- MCP servers: `/home/chad/mcp-servers/`
- Test results: `/home/chad/repos/witchcityrope/test-*.png`

### Configuration
- Git config: `~/.gitconfig`
- .NET SDK: `~/.dotnet/`
- npm packages: `/home/chad/repos/witchcityrope/node_modules/`

### Documentation
- Main docs: `/home/chad/repos/witchcityrope/WitchCityRope/CLAUDE.md`
- MCP setup: `/home/chad/repos/witchcityrope/WitchCityRope/docs/MCP_UBUNTU_SETUP.md`
- UI testing: `/home/chad/repos/witchcityrope/WitchCityRope/docs/UI_TESTING_UBUNTU.md`
- This file: `/home/chad/repos/witchcityrope/WitchCityRope/docs/UBUNTU_QUICK_REFERENCE.md`

## Troubleshooting

### Chrome DevTools Connection
```bash
# Check if Chrome is running
ps aux | grep chrome | grep 9222

# Test connection
curl http://localhost:9222/json/version

# Kill and restart Chrome
pkill -f chrome
google-chrome --remote-debugging-port=9222
```

### .NET Issues
```bash
# Verify .NET 9.0
~/.dotnet/dotnet --version

# Add to PATH permanently
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

### Port Conflicts
```bash
# Check what's using a port
lsof -i :5000
lsof -i :9222

# Kill process using port
kill -9 $(lsof -t -i:5000)
```

## Environment Details
- **OS**: Ubuntu 24.04 LTS (Native - NOT WSL)
- **.NET**: 9.0.301 (at ~/.dotnet)
- **Node.js**: v22.17.0
- **PostgreSQL**: 16
- **Docker**: 27.5.1
- **Chrome**: 138.0.7204.92
- **Python**: 3.12.3