# Session Notes - MCP Server Development in WSL2

## Date: 2025-06-30

## Key Discoveries

### 1. Chrome Access from WSL2
- **Problem**: Cannot directly launch Windows Chrome from WSL2 using standard paths
- **Root Cause**: WSL2 runs in a lightweight VM with its own network stack, isolated from Windows host
- **Discovery**: Windows executables can be accessed via `/mnt/c/` but GUI applications require special handling

### 2. PowerShell Bridge Solution
- **Working Solution**: Using PowerShell.exe as a bridge to launch Windows applications
- **Command**: `powershell.exe -Command "Start-Process 'chrome.exe' -ArgumentList 'http://localhost:3000'"`
- **Why it works**: PowerShell runs in Windows context and can properly launch GUI applications

### 3. WSL2 Networking Architecture
- **localhost**: In WSL2, localhost refers to the WSL2 VM, not the Windows host
- **Port forwarding**: WSL2 automatically forwards ports from WSL2 to Windows host
- **Accessing Windows from WSL2**: Use `$(hostname).local` or the Windows host IP

## Solutions Attempted

### ✅ What Worked
1. **PowerShell Bridge Method**
   ```bash
   powershell.exe -Command "Start-Process 'chrome.exe' -ArgumentList 'http://localhost:3000'"
   ```

2. **Direct PowerShell Chrome Launch**
   ```bash
   powershell.exe Start-Process chrome http://localhost:3000
   ```

3. **Using cmd.exe**
   ```bash
   cmd.exe /c start chrome http://localhost:3000
   ```

### ❌ What Didn't Work
1. **Direct Chrome executable access**
   - `/mnt/c/Program Files/Google/Chrome/Application/chrome.exe`
   - Failed due to GUI application limitations in WSL2

2. **WSL-specific tools**
   - `wslview` - Not installed by default
   - `xdg-open` - Requires X server configuration

3. **Simple aliasing**
   - Creating bash aliases to Windows executables doesn't solve the GUI problem

## Important File Locations

### Project Structure
```
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/
├── src/
│   └── filesystem/
│       ├── index.ts          # Main MCP server implementation
│       └── package.json      # Dependencies and scripts
├── dist/                     # Compiled JavaScript output
├── build.js                  # Build script for TypeScript compilation
└── SESSION_NOTES.md          # This file
```

### Key Configurations
- **TypeScript Config**: Configured for Node.js ES modules
- **Package.json**: Contains MCP server metadata and scripts
- **Build Output**: `dist/index.js` - The compiled server

## Lessons Learned About WSL2 Networking

1. **Port Forwarding is Automatic**
   - Services running on WSL2 localhost are automatically accessible from Windows
   - No manual port forwarding configuration needed for most cases

2. **Localhost Context Matters**
   - In WSL2: `localhost` = WSL2 VM
   - In Windows: `localhost` = Windows host (but includes forwarded WSL2 ports)

3. **GUI Applications Require Special Handling**
   - Cannot run Windows GUI apps directly from WSL2 bash
   - Must use Windows command interpreters (PowerShell, cmd) as bridges

4. **Network Isolation**
   - WSL2 has its own virtual network adapter
   - Communication happens through Hyper-V virtual switch

## Next Steps for Future Sessions

1. **Development Environment**
   - Consider installing `wslu` package for better Windows integration
   - Set up permanent aliases for common Windows applications

2. **MCP Server Development**
   - Test the filesystem server with Claude Desktop
   - Implement error handling for edge cases
   - Add more file operation capabilities

3. **Debugging Tools**
   - Set up proper logging for the MCP server
   - Consider using Chrome DevTools for debugging

4. **Documentation**
   - Create user guide for the filesystem MCP server
   - Document the MCP protocol implementation details

5. **Optimization**
   - Implement caching for frequently accessed files
   - Add rate limiting for file operations
   - Consider security implications of file access

## Quick Reference Commands

```bash
# Build the TypeScript project
npm run build

# Open Chrome from WSL2
powershell.exe Start-Process chrome http://localhost:3000

# Check if server is running
curl http://localhost:3000

# Find Windows host IP from WSL2
ip route | grep default | awk '{print $3}'
```

## Security Considerations
- The filesystem MCP server provides unrestricted file access
- Should implement path validation and access controls
- Consider sandboxing file operations to specific directories

---
*This document serves as a reference for future development sessions on the MCP filesystem server project.*