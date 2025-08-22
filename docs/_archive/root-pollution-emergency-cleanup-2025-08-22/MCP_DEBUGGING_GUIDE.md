# MCP Server Debugging Guide

## Overview

The MCP (Model Context Protocol) server provides Claude Desktop with the ability to execute system commands for debugging purposes. This is particularly useful for testing API endpoints, checking service health, and troubleshooting issues without leaving the Claude interface.

## Configuration

The MCP server is configured in Claude Desktop's `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "commands": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-commands"],
      "env": {
        "ALLOWED_COMMANDS": "curl,powershell"
      }
    }
  }
}
```

## Available Commands

### 1. curl - HTTP Testing Tool

Use `curl` for testing API endpoints, checking service health, and debugging HTTP issues.

#### Basic API Testing
```bash
# Test if API is running
curl http://localhost:7000/health

# Test HTTPS endpoint
curl -k https://localhost:7001/api/v1/health

# Get all events
curl http://localhost:7000/api/v1/events

# Test with authentication header
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" http://localhost:7000/api/v1/users/profile
```

#### POST Requests
```bash
# Login request
curl -X POST http://localhost:7000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Admin123!"}'

# Register new user
curl -X POST http://localhost:7000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"newuser@example.com","password":"Test123!","sceneName":"NewUser","acceptTerms":true}'

# Create event (requires auth)
curl -X POST http://localhost:7000/api/v1/events \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Event","description":"Testing","startDate":"2025-07-01T18:00:00Z","capacity":20}'
```

#### Debugging Options
```bash
# Show headers only
curl -I http://localhost:7000/health

# Verbose output (see full request/response)
curl -v http://localhost:7000/api/v1/events

# Follow redirects
curl -L http://localhost:5000

# Set timeout (in seconds)
curl --max-time 5 http://localhost:7000/health

# Save response to file
curl -o response.json http://localhost:7000/api/v1/events
```

### 2. PowerShell - System Debugging

Use PowerShell for Windows system operations and process management.

#### Process Management
```powershell
# List all dotnet processes
powershell -Command "Get-Process | Where-Object {$_.ProcessName -like '*dotnet*'}"

# Kill stuck processes
powershell -Command "Get-Process dotnet | Stop-Process -Force"

# Check memory usage
powershell -Command "Get-Process dotnet | Select-Object Name,@{n='Memory(MB)';e={[math]::Round($_.WorkingSet64/1MB,2)}}"
```

#### Port Management
```powershell
# Check if ports are in use
powershell -Command "Get-NetTCPConnection | Where-Object {$_.LocalPort -in @(5000,5001,7000,7001)} | Select-Object LocalPort,State,OwningProcess"

# Find process using specific port
powershell -Command "Get-Process -Id (Get-NetTCPConnection -LocalPort 7000).OwningProcess"

# Check all listening ports
powershell -Command "Get-NetTCPConnection -State Listen | Select-Object LocalPort,State | Sort-Object LocalPort"
```

#### File System Operations
```powershell
# Check if database exists
powershell -Command "Test-Path 'src/WitchCityRope.Api/witchcityrope.db'"

# Get file size
powershell -Command "(Get-Item 'src/WitchCityRope.Api/witchcityrope.db').Length / 1MB"

# List recent log files
powershell -Command "Get-ChildItem -Path logs -Filter *.log | Sort-Object LastWriteTime -Descending | Select-Object -First 10"

# Check disk space
powershell -Command "Get-PSDrive C | Select-Object Used,Free,@{n='Free(GB)';e={[math]::Round($_.Free/1GB,2)}}"
```

#### Service Status
```powershell
# Check if IIS is running
powershell -Command "Get-Service W3SVC"

# Check Docker status
powershell -Command "docker ps"

# Check if SQL Server is running (if using)
powershell -Command "Get-Service MSSQLSERVER"
```

## Common Debugging Scenarios

### 1. Service Won't Start
```bash
# Check if ports are available
powershell -Command "Get-NetTCPConnection -LocalPort 7000"

# Check for running processes
powershell -Command "Get-Process dotnet"

# Test basic connectivity
curl http://localhost:7000/health
```

### 2. Authentication Issues
```bash
# Test login
curl -X POST http://localhost:7000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Test with token
curl -H "Authorization: Bearer TOKEN" http://localhost:7000/api/v1/users/profile
```

### 3. Database Issues
```powershell
# Check if database file exists
powershell -Command "Test-Path 'src/WitchCityRope.Api/witchcityrope.db'"

# Check file permissions
powershell -Command "Get-Acl 'src/WitchCityRope.Api/witchcityrope.db' | Select-Object Owner,AccessToString"
```

### 4. Performance Issues
```powershell
# Check CPU usage
powershell -Command "Get-Process dotnet | Select-Object Name,CPU"

# Check memory usage over time
powershell -Command "while($true){Get-Process dotnet | Select-Object Name,@{n='Memory(MB)';e={[math]::Round($_.WorkingSet64/1MB,2)}},CPU; Start-Sleep -Seconds 5}"
```

### 5. External Service Testing
```bash
# Test SendGrid (with API key)
curl -X POST https://api.sendgrid.com/v3/mail/send \
  -H "Authorization: Bearer YOUR_SENDGRID_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"personalizations":[{"to":[{"email":"test@example.com"}]}],"from":{"email":"noreply@witchcityrope.com"},"subject":"Test","content":[{"type":"text/plain","value":"Test email"}]}'

# Test PayPal sandbox
curl -X POST https://api-m.sandbox.paypal.com/v2/checkout/orders \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"intent":"CAPTURE","purchase_units":[{"amount":{"currency_code":"USD","value":"10.00"}}]}'
```

## Best Practices

1. **Security**: Never include real API keys or passwords in commands
2. **Logging**: Save command outputs for later analysis
3. **Cleanup**: Kill stuck processes to free up ports
4. **Monitoring**: Use PowerShell loops for continuous monitoring
5. **Documentation**: Document any unusual findings or solutions

## Limitations

- Only `curl` and `powershell` commands are allowed
- Commands run in the context of the Claude Desktop application
- No access to privileged operations without elevation
- Output is limited to console text (no GUI applications)

## Quick Reference

```bash
# Most common commands
curl http://localhost:7000/health                    # Check API health
curl http://localhost:5000                           # Check Web UI
powershell -Command "Get-Process dotnet"             # List .NET processes
powershell -Command "Get-NetTCPConnection -LocalPort 7000" # Check port usage
```