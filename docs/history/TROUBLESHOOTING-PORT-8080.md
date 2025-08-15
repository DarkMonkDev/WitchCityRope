# Troubleshooting: VS Code Opens Wrong Port (8080)

## Issue
When pressing F5 in VS Code, the browser opens to http://localhost:8080 instead of the correct http://localhost:8280.

## Root Causes
1. **Cached build output** - Old appsettings.json in bin folder
2. **Docker settings bleeding through** - Port 8080 is the Docker internal port
3. **VS Code launch configuration** not being respected

## Solution Steps

### 1. Clean Everything
```bash
cd /home/chad/repos/witchcityrope/WitchCityRope
dotnet clean
rm -rf src/*/bin src/*/obj
pkill -f "dotnet.*WitchCityRope" || true
```

### 2. Verify Configuration Files

#### Check appsettings.Development.json
```json
{
  "ApiUrl": "http://localhost:8180"
}
```

#### Check launchSettings.json
```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:8280"
    }
  }
}
```

#### Check .vscode/launch.json
```json
{
  "env": {
    "ASPNETCORE_URLS": "http://localhost:8280"
  }
}
```

### 3. Force Correct URL in VS Code

If VS Code still opens the wrong URL, you can:

1. **Close VS Code completely**
2. **Clear VS Code workspace cache**:
   ```bash
   rm -rf ~/.config/Code/Workspaces/*
   ```
3. **Restart VS Code**
4. **Press F5 again**

### 4. Alternative: Use Terminal

If VS Code continues to have issues, run directly:
```bash
# Terminal 1
cd src/WitchCityRope.Api
dotnet run --urls "http://localhost:8180"

# Terminal 2
cd src/WitchCityRope.Web
export ApiUrl="http://localhost:8180"
dotnet run --urls "http://localhost:8280"
```

Then manually open: http://localhost:8280

### 5. Check for Port Conflicts

Make sure nothing else is using port 8080:
```bash
lsof -i :8080
# If something is using it:
kill -9 <PID>
```

### 6. VS Code Debug Output

When you press F5, check the Debug Console in VS Code. You should see:
```
Now listening on: http://localhost:8280
```

If you see http://localhost:8080, then the environment variables aren't being applied.

## Prevention

1. **Always use the provided VS Code configuration**
2. **Don't mix Docker and local development** in the same session
3. **Clean build output** when switching between Docker and local
4. **Use the run-local.sh script** for consistent behavior

## If All Else Fails

Create a custom launch profile that explicitly sets the URL:

1. Edit `.vscode/launch.json`
2. Add to the args array:
   ```json
   "args": ["--urls", "http://localhost:8280"]
   ```
3. This forces dotnet to use the correct URL regardless of other settings