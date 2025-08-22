# CLAUDE CODE ENVIRONMENT INFORMATION

## 🖥️ YOU ARE RUNNING IN WSL TERMINAL ON WINDOWS

### Environment Details:
- **Operating System**: WSL2 Ubuntu 24.04 running on Windows host
- **Working Directory**: `/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers`
- **Platform**: Linux (WSL2)
- **Kernel**: 5.15.146.1-microsoft-standard-WSL2

### Key Path Translations:
| WSL Path | Windows Path |
|----------|--------------|
| `/mnt/c/` | `C:\` |
| `/mnt/c/users/chad/` | `C:\Users\chad\` |
| `/mnt/c/users/chad/source/repos/` | `C:\Users\chad\source\repos\` |
| `/mnt/c/users/chad/AppData/Roaming/Claude/` | `C:\Users\chad\AppData\Roaming\Claude\` |

### Important Notes:
- **MCP Servers**: All MCP (Model Context Protocol) servers run through the Windows Claude Desktop application
- **File Access**: You have direct access to Windows filesystem through `/mnt/c/`
- **Path Format**: Always use forward slashes (/) in WSL, even when accessing Windows files
- **Case Sensitivity**: WSL is case-sensitive, Windows paths accessed through WSL preserve their original case

### Additional Working Directories Available:
- `/mnt/c/users/chad/AppData/Roaming/Claude`
- `/tmp` (WSL temporary directory)
- `/home/user` (WSL home)
- `/home/chad` (WSL home)
- `/mnt/c/Users/chad/source/repos/WitchCityRope` (Project root)

---

## Project Documentation

This directory contains MCP server configurations and browser automation tools for the WitchCityRope project.

### Key Components:
- Browser automation scripts and configurations
- MCP server test results and documentation
- Node.js-based browser server implementation
- PowerShell bridge scripts for Windows integration

### Quick Reference:
- Browser MCP documentation: See `README_BROWSER_MCP.md`
- Browser automation guides: See `BROWSER_AUTOMATION_*.md` files
- Test scripts: Various `test-*.js` and `test-*.sh` files
- Server implementation: `browser-server-persistent/` directory