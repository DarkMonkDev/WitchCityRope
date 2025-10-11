# MCP Server Setup Guide
<!-- Last Updated: 2025-10-10 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

This guide provides step-by-step instructions for setting up Model Context Protocol (MCP) servers for the WitchCityRope project. MCP servers enable Claude Code to interact with external services and tools for enhanced development capabilities.

## Why MCP Servers?

MCP servers extend Claude Code's capabilities by providing:

- **Context7**: Real-time library documentation access for current package versions
- **DigitalOcean**: Cloud infrastructure management for production deployment
- **SSH**: Remote server access and management capabilities
- **FileSystem**: Secure file operations within allowed paths
- **GitHub**: Repository management, issues, and pull requests
- **Memory**: Project knowledge storage across sessions
- **Docker**: Container management (via CLI recommended)

## MCP Servers Used in WitchCityRope

### 1. Context7 MCP Server
**Purpose**: Provides up-to-date documentation for libraries and frameworks

**Why Needed**:
- Claude's knowledge cutoff is January 2025
- Libraries like Mantine, TanStack Query, and Vite evolve rapidly
- Ensures accurate API usage and pattern recommendations
- Reduces errors from deprecated APIs

**Use Cases**:
- Looking up current Mantine v7 component APIs
- Checking TanStack Query v5 hooks
- Verifying Vite configuration options
- React 18+ features and patterns

### 2. DigitalOcean MCP Server
**Purpose**: Manages cloud infrastructure programmatically

**Why Needed**:
- Production deployment to DigitalOcean Droplets ($92/month architecture)
- Automated infrastructure provisioning
- Droplet management and monitoring
- Database and storage management
- Cost optimization and resource tracking

**Use Cases**:
- Creating and managing droplets for production/staging
- Setting up managed PostgreSQL databases
- Configuring container registries
- Monitoring resource usage and costs
- Managing DNS records and domains

**Reference**: See [Deployment Handoff Master](/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/handoffs/DEPLOYMENT-HANDOFF-MASTER.md) for deployment architecture details.

### 3. SSH MCP Server
**Purpose**: Enables secure shell access to remote servers

**Why Needed**:
- Remote server management and debugging
- Log file inspection on production servers
- Emergency troubleshooting and hotfixes
- Database operations on production systems
- Performance monitoring and diagnostics

**Use Cases**:
- Connecting to DigitalOcean droplets for configuration
- Debugging production issues in real-time
- Executing maintenance scripts remotely
- Collecting logs and diagnostics
- Managing Docker containers on remote servers

**Security Note**: SSH access should be restricted to authorized team members only with proper key-based authentication.

## Prerequisites

Before setting up MCP servers, ensure you have:

- [ ] Claude Code installed and configured
- [ ] Node.js 18+ installed for Context7
- [ ] Valid accounts for services (DigitalOcean, GitHub, etc.)
- [ ] SSH keys generated for server access
- [ ] Administrative access to your development machine

## Setup Instructions

### 1. Context7 MCP Server Setup

#### Installation
```bash
# Install Context7 globally
npm install -g @context7/mcp-server

# Verify installation
context7 --version
```

#### Configuration
Add to your MCP configuration file:

```json
{
  "mcpServers": {
    "context7": {
      "command": "npx",
      "args": ["-y", "@context7/mcp-server"],
      "disabled": false
    }
  }
}
```

#### Usage in Claude Code
```plaintext
To use Context7, add "use context7" to your prompt:

Example: "use context7 to find the latest Mantine Button API"
```

#### Verification
Test Context7 is working:
```plaintext
Prompt: "use context7 to show me Mantine v7 Select component API"

Expected: Claude retrieves current documentation for Mantine Select
```

### 2. DigitalOcean MCP Server Setup

#### Prerequisites
- DigitalOcean account with active billing
- Access to API settings in DigitalOcean dashboard

#### Get DigitalOcean API Token

**IMPORTANT**: Never commit API tokens to version control!

1. **Login to DigitalOcean**
   - Go to https://cloud.digitalocean.com
   - Navigate to API section (left sidebar)

2. **Generate New Token**
   - Click "Generate New Token"
   - Name: "WitchCityRope-MCP-Server"
   - Scopes: Select "Read" and "Write" (full access)
   - Expiration: Set appropriate expiration (90 days recommended)
   - Click "Generate Token"

3. **Copy Token Immediately**
   - Token is shown ONCE - copy it immediately
   - Store in secure password manager
   - Never share or commit to git

4. **Store Securely**
   ```bash
   # Store in environment variable (Linux/macOS)
   export DIGITALOCEAN_TOKEN="dop_v1_your_token_here"

   # Add to ~/.bashrc or ~/.zshrc for persistence
   echo 'export DIGITALOCEAN_TOKEN="dop_v1_your_token_here"' >> ~/.bashrc
   ```

#### Configuration
Add to your MCP configuration file:

```json
{
  "mcpServers": {
    "digitalocean": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-digitalocean"],
      "env": {
        "DIGITALOCEAN_TOKEN": "${DIGITALOCEAN_TOKEN}"
      },
      "disabled": false
    }
  }
}
```

#### Verification
Test DigitalOcean MCP:
```bash
# List droplets (should work if token is valid)
# This would be done through Claude Code MCP interface
```

**Expected**: Returns list of droplets or empty array if none exist

### 3. SSH MCP Server Setup

#### Prerequisites
- SSH key pair generated
- Remote server accessible via SSH
- Server IP address or hostname

#### Generate SSH Keys (if needed)
```bash
# Generate new SSH key pair
ssh-keygen -t ed25519 -C "witchcityrope-dev@example.com"

# Location: ~/.ssh/id_ed25519 (private) and ~/.ssh/id_ed25519.pub (public)
# Set passphrase for additional security (recommended)
```

#### Add Public Key to Remote Server
```bash
# Copy public key to remote server
ssh-copy-id -i ~/.ssh/id_ed25519.pub user@remote-server-ip

# Or manually add to ~/.ssh/authorized_keys on remote server
```

#### Configuration
Add to your MCP configuration file:

```json
{
  "mcpServers": {
    "ssh": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-ssh"],
      "env": {
        "SSH_PRIVATE_KEY_PATH": "/home/yourusername/.ssh/id_ed25519"
      },
      "disabled": false
    }
  }
}
```

**Security Configuration**:
```bash
# Restrict SSH key permissions (CRITICAL)
chmod 600 ~/.ssh/id_ed25519
chmod 644 ~/.ssh/id_ed25519.pub

# Verify permissions
ls -la ~/.ssh/
```

#### Verification
Test SSH connection:
```bash
# Test SSH connection manually first
ssh -i ~/.ssh/id_ed25519 user@remote-server-ip

# Expected: Successful login to remote server
```

## Complete MCP Configuration Example

Example MCP configuration file with placeholder tokens:

```json
{
  "mcpServers": {
    "context7": {
      "command": "npx",
      "args": ["-y", "@context7/mcp-server"],
      "disabled": false
    },
    "digitalocean": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-digitalocean"],
      "env": {
        "DIGITALOCEAN_TOKEN": "${DIGITALOCEAN_TOKEN}"
      },
      "disabled": false
    },
    "ssh": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-ssh"],
      "env": {
        "SSH_PRIVATE_KEY_PATH": "/home/yourusername/.ssh/id_ed25519"
      },
      "disabled": false
    },
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem"],
      "env": {
        "ALLOWED_PATHS": "/home/yourusername/repos/witchcityrope-react,/home/yourusername/mcp-servers"
      },
      "disabled": false
    },
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_TOKEN": "${GITHUB_TOKEN}"
      },
      "disabled": false
    },
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"],
      "disabled": false
    }
  }
}
```

## Verification Steps

### Test All MCP Servers

1. **Context7 Test**
   ```plaintext
   Prompt: "use context7 to show me React 18 hooks documentation"
   Expected: Returns current React hooks documentation
   ```

2. **DigitalOcean Test**
   ```plaintext
   Prompt: "List my DigitalOcean droplets"
   Expected: Returns list of droplets or empty array
   ```

3. **SSH Test**
   ```plaintext
   Prompt: "Connect to [server-ip] and show disk usage"
   Expected: Successfully connects and shows df -h output
   ```

4. **FileSystem Test**
   ```plaintext
   Prompt: "List files in /home/yourusername/repos/witchcityrope-react"
   Expected: Returns directory listing
   ```

5. **GitHub Test**
   ```plaintext
   Prompt: "Show recent commits on main branch"
   Expected: Returns recent commit history
   ```

6. **Memory Test**
   ```plaintext
   Prompt: "Remember that our production database uses PostgreSQL 16"
   Expected: Stores information for future sessions
   ```

## Security Best Practices

### Token Management

**NEVER commit tokens to git**:
```bash
# Add to .gitignore (already configured in project)
.env
.env.local
*.token
*_token
```

**Use environment variables**:
```bash
# Linux/macOS - Add to ~/.bashrc or ~/.zshrc
export DIGITALOCEAN_TOKEN="dop_v1_your_token_here"
export GITHUB_TOKEN="ghp_your_token_here"

# Reload configuration
source ~/.bashrc
```

**Token rotation**:
- Rotate API tokens every 90 days
- Immediately revoke compromised tokens
- Use separate tokens for development and production
- Document token purposes and expiration dates

### SSH Security

**Key-based authentication only**:
```bash
# Disable password authentication on remote server
# Edit /etc/ssh/sshd_config
PasswordAuthentication no
PubkeyAuthentication yes
```

**SSH key permissions**:
```bash
# Private key MUST be 600 (read/write owner only)
chmod 600 ~/.ssh/id_ed25519

# Public key should be 644 (readable by all)
chmod 644 ~/.ssh/id_ed25519.pub

# SSH directory should be 700
chmod 700 ~/.ssh
```

**Use passphrase-protected keys**:
- Always set passphrase when generating SSH keys
- Use ssh-agent to avoid repeated passphrase entry
- Never share private keys

### Access Control

**Principle of least privilege**:
- Grant minimum required permissions
- Use read-only tokens when possible
- Restrict SSH access to necessary servers only
- Regularly audit access logs

**Team coordination**:
- Document who has which access levels
- Revoke access immediately when team members leave
- Use separate credentials per team member (no sharing)
- Implement 2FA on all service accounts

## Troubleshooting

### MCP Server Not Responding

**Symptoms**: MCP commands timeout or fail

**Solutions**:
```bash
# Check if MCP server is running
ps aux | grep mcp

# Restart Claude Code to reload MCP configuration
# Kill and restart the application

# Check logs (location varies by OS)
tail -f ~/.config/claude-code/logs/mcp.log
```

### DigitalOcean Token Invalid

**Symptoms**: "401 Unauthorized" or "Invalid token" errors

**Solutions**:
1. Verify token is correctly set in environment:
   ```bash
   echo $DIGITALOCEAN_TOKEN
   # Should show: dop_v1_...
   ```

2. Check token hasn't expired in DigitalOcean dashboard

3. Regenerate token if needed and update configuration

4. Verify token has required permissions (Read + Write)

### SSH Connection Failed

**Symptoms**: "Permission denied" or "Connection refused"

**Solutions**:
```bash
# Test SSH connection manually
ssh -vvv -i ~/.ssh/id_ed25519 user@server

# Check key permissions
ls -la ~/.ssh/id_ed25519
# Should show: -rw------- (600)

# Verify public key is on remote server
ssh user@server "cat ~/.ssh/authorized_keys"

# Check SSH service on remote server
ssh user@server "sudo systemctl status ssh"
```

### Context7 Returns Outdated Info

**Symptoms**: Documentation doesn't match current package version

**Solutions**:
```bash
# Update Context7 to latest version
npm update -g @context7/mcp-server

# Clear Context7 cache (if applicable)
rm -rf ~/.context7/cache

# Specify exact version in prompt
# Example: "use context7 to show Mantine v7.12.0 Button API"
```

## Environment-Specific Configuration

### Development Environment
```json
{
  "mcpServers": {
    "digitalocean": {
      "disabled": false,
      "env": {
        "DIGITALOCEAN_TOKEN": "${DIGITALOCEAN_DEV_TOKEN}"
      }
    }
  }
}
```

### Production Environment
```json
{
  "mcpServers": {
    "digitalocean": {
      "disabled": false,
      "env": {
        "DIGITALOCEAN_TOKEN": "${DIGITALOCEAN_PROD_TOKEN}"
      }
    }
  }
}
```

**Best Practice**: Use separate tokens for development and production to limit blast radius of token compromise.

## Integration with WitchCityRope Workflow

### Development Workflow

1. **Start Development Session**
   - Context7 available for library documentation
   - FileSystem MCP for code access
   - GitHub MCP for repository operations

2. **Deploy to Staging**
   - DigitalOcean MCP provisions staging resources
   - SSH MCP connects for configuration
   - Docker MCP manages containers (via CLI)

3. **Production Deployment**
   - DigitalOcean MCP manages production droplet
   - SSH MCP for production hotfixes (emergency only)
   - Memory MCP tracks deployment status

### Deployment Workflow Integration

MCP servers integrate with the [DigitalOcean Deployment Plan](/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/handoffs/DEPLOYMENT-HANDOFF-MASTER.md):

- **Phase 1 (Infrastructure Setup)**: DigitalOcean MCP provisions droplets
- **Phase 2 (Application Deployment)**: SSH MCP configures Docker environment
- **Phase 3 (CI/CD Configuration)**: GitHub MCP sets up actions
- **Phase 4 (Testing)**: SSH MCP validates production setup
- **Phase 5 (Monitoring)**: SSH MCP accesses logs and metrics

## Additional Resources

### MCP Server Documentation
- Context7: https://www.npmjs.com/package/@context7/mcp-server
- DigitalOcean MCP: https://modelcontextprotocol.io/servers/digitalocean
- SSH MCP: https://modelcontextprotocol.io/servers/ssh

### WitchCityRope Documentation
- [Deployment Handoff Master](/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/handoffs/DEPLOYMENT-HANDOFF-MASTER.md)
- [Docker Dev Guide](/DOCKER_DEV_GUIDE.md)
- [Architecture Documentation](/ARCHITECTURE.md)

### Security Resources
- DigitalOcean API Security: https://docs.digitalocean.com/reference/api/
- SSH Key Management: https://www.ssh.com/academy/ssh/keygen
- Environment Variable Security: https://12factor.net/config

## Maintenance

### Regular Tasks

**Weekly**:
- Verify all MCP servers responding
- Check for MCP server package updates
- Review access logs for anomalies

**Monthly**:
- Rotate API tokens (every 90 days)
- Review and prune old SSH keys
- Update MCP server packages
- Audit team access levels

**Quarterly**:
- Full security audit of all MCP configurations
- Review and update this documentation
- Verify backup and recovery procedures
- Test all emergency access procedures

## Support

### Getting Help

1. **Check Troubleshooting Section**: Most common issues covered above
2. **Review MCP Server Logs**: Check ~/.config/claude-code/logs/
3. **Consult Official Documentation**: Links provided in Additional Resources
4. **Team Support**: Contact technical lead for assistance

### Reporting Issues

When reporting MCP server issues, include:
- MCP server name (Context7, DigitalOcean, SSH, etc.)
- Error message (full text)
- Steps to reproduce
- Environment details (OS, Claude Code version)
- Recent configuration changes

---

**Document Prepared By**: Librarian Agent
**Created**: October 10, 2025
**Status**: Active
**Next Review**: January 10, 2026

*This guide provides comprehensive setup instructions for all MCP servers used in WitchCityRope development. For deployment-specific MCP usage, refer to the [Deployment Handoff Master](/docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/handoffs/DEPLOYMENT-HANDOFF-MASTER.md).*
