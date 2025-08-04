# MCP (Model Context Protocol) Servers Documentation

This document provides comprehensive information about all MCP servers configured for the WitchCityRope project.

## Table of Contents
1. [Overview](#overview)
2. [Installed MCP Servers](#installed-mcp-servers)
3. [Configuration Details](#configuration-details)
4. [Usage Examples](#usage-examples)
5. [Troubleshooting](#troubleshooting)
6. [Security Considerations](#security-considerations)

## Overview

MCP (Model Context Protocol) servers extend Claude's capabilities by providing access to external tools and services. The WitchCityRope project uses multiple MCP servers for various development and automation tasks.

## Installed MCP Servers

| Server | Purpose | Package/Command |
|--------|---------|-----------------|
| Commands | Execute system commands (curl, powershell) | `mcp-server-commands` |
| Browser Tools | Browser automation and web scraping | `browser-tools-mcp` |
| GitHub | GitHub API integration | `@modelcontextprotocol/server-github` |
| PostgreSQL | Database queries and management | `@modelcontextprotocol/server-postgres` |
| FileSystem | File system access and manipulation | `@modelcontextprotocol/server-filesystem` |
| Memory | Persistent memory/knowledge graph | `@modelcontextprotocol/server-memory` |
| Stagehand | AI-powered browser automation | Custom build from source |
| Docker | Docker container management | `mcp-server-docker` |

## Configuration Details

### 1. Commands Server
```json
{
  "command": "npx",
  "args": ["-y", "mcp-server-commands"],
  "env": {
    "ALLOWED_COMMANDS": "curl,powershell"
  }
}
```
- Allows execution of curl and powershell commands
- Useful for API testing and system automation

### 2. Browser Tools
```json
{
  "command": "npx",
  "args": ["-y", "browser-tools-mcp"],
  "env": {
    "NODE_ENV": "production",
    "DEBUG": ""
  }
}
```
- Provides browser automation capabilities
- Runs in production mode without debug output

### 3. GitHub Server
```json
{
  "command": "npx",
  "args": ["-y", "@modelcontextprotocol/server-github"],
  "env": {
    "GITHUB_TOKEN": "github_pat_[...]"
  }
}
```
- **Security Warning**: GitHub token is exposed in config
- Provides access to GitHub API for repository management
- Can create issues, PRs, and manage repositories

### 4. PostgreSQL Server
```json
{
  "command": "npx",
  "args": [
    "-y",
    "@modelcontextprotocol/server-postgres",
    "postgresql://postgres:your_password_here@localhost:5432/witchcityrope_db"
  ]
}
```
- Connection string: `postgresql://postgres:your_password_here@localhost:5432/witchcityrope_db`
- **Note**: Password needs to be updated before use
- Provides read-only database access for safety

### 5. FileSystem Server
```json
{
  "command": "npx",
  "args": [
    "-y",
    "@modelcontextprotocol/server-filesystem",
    "C:\\Users\\chad\\source\\repos",
    "C:\\Users\\chad\\Documents",
    "C:\\Users\\chad\\Downloads",
    "C:\\Users\\chad\\Desktop"
  ]
}
```
- Restricted access to specific directories:
  - Source repositories
  - Documents folder
  - Downloads folder
  - Desktop folder

### 6. Memory Server
```json
{
  "command": "npx",
  "args": ["-y", "@modelcontextprotocol/server-memory"],
  "env": {
    "MEMORY_FILE_PATH": "C:\\Users\\chad\\AppData\\Roaming\\Claude\\memory-data\\memory.json"
  }
}
```
- Stores persistent memory in JSON format
- Located in Claude's AppData folder
- Provides knowledge graph capabilities

### 7. Stagehand Server
```json
{
  "command": "node",
  "args": ["C:\\Users\\chad\\source\\repos\\WitchCityRope\\mcp-server-browserbase\\stagehand\\dist\\index.js"],
  "env": {
    "OPENAI_API_KEY": "sk-proj-[...]",
    "LOCAL_CDP_URL": "http://localhost:9222"
  }
}
```
- Uses OpenAI API for natural language browser automation
- Connects to Chrome DevTools Protocol on port 9222
- **Note**: Requires Chrome to be started in debug mode

### 8. Docker Server
```json
{
  "command": "npx",
  "args": ["-y", "mcp-server-docker"],
  "env": {
    "ALLOWED_CONTAINERS": "*",
    "DEFAULT_SERVICE": ""
  }
}
```
- Allows access to all containers (`*`)
- No default service specified
- Provides Docker container management capabilities

## Usage Examples

### Commands Server
```
# Execute a curl command
curl https://api.example.com/data

# Run a PowerShell script
powershell -Command "Get-Process | Select-Object -First 5"
```

### GitHub Server
```
# Create a new issue
gh issue create --title "Bug report" --body "Description"

# List pull requests
gh pr list --state open
```

### PostgreSQL Server
```sql
-- Query events table
SELECT * FROM events WHERE start_date > NOW() ORDER BY start_date;

-- Get user statistics
SELECT role, COUNT(*) FROM users GROUP BY role;
```

### FileSystem Server
```
# Read a file
Read file from C:\Users\chad\source\repos\WitchCityRope\README.md

# List directory contents
List files in C:\Users\chad\Documents
```

### Docker Server
```
# List running containers
docker ps

# View container logs
docker logs <container_name>

# Execute command in container
docker exec <container_name> <command>
```

## Troubleshooting

### Common Issues

1. **MCP Server Not Loading**
   - Restart Claude Desktop
   - Check if Node.js is installed
   - Verify npm packages are accessible

2. **PostgreSQL Connection Failed**
   - Update the connection string with correct credentials
   - Ensure PostgreSQL is running
   - Check firewall settings

3. **Stagehand Not Working**
   - Start Chrome with: `chrome.exe --remote-debugging-port=9222`
   - Verify OpenAI API key is valid
   - Check if the built JavaScript file exists

4. **FileSystem Access Denied**
   - Verify paths are included in the configuration
   - Check Windows file permissions

5. **Docker Commands Failing**
   - Ensure Docker Desktop is running
   - Check if current user has Docker permissions

### Debug Mode

To enable debug output for any MCP server, add to the environment:
```json
"env": {
  "DEBUG": "*"
}
```

## Security Considerations

1. **API Keys and Tokens**
   - GitHub token and OpenAI API key are exposed in the config
   - Consider using environment variables instead
   - Rotate keys regularly
   - Never commit keys to version control

2. **FileSystem Access**
   - Limited to specific directories
   - Cannot access system directories
   - Be cautious when granting write permissions

3. **Database Access**
   - PostgreSQL server provides read-only access
   - Use strong passwords
   - Consider connection encryption for production

4. **Docker Access**
   - Currently allows access to all containers
   - Consider restricting to specific containers in production
   - Be cautious with container execution commands

## Best Practices

1. **Regular Updates**
   - Keep MCP servers updated using `-y` flag with npx
   - Monitor for security updates

2. **Logging**
   - Enable logging for troubleshooting
   - Monitor logs for suspicious activity

3. **Access Control**
   - Limit file system access to necessary directories
   - Restrict command execution to safe commands
   - Use read-only database access where possible

4. **Backup**
   - Backup memory server data regularly
   - Keep configuration backups
   - Document any custom configurations

## Additional Resources

- [MCP Protocol Documentation](https://github.com/modelcontextprotocol/protocol)
- [Claude Desktop Documentation](https://claude.ai/docs)
- [Individual MCP Server Repositories](https://github.com/modelcontextprotocol)