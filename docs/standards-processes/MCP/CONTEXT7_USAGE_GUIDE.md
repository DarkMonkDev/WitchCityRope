# Context7 MCP Usage Guide for WitchCityRope

## Overview

Context7 is a Model Context Protocol (MCP) server that provides up-to-date, version-specific documentation directly within Claude Code sessions. This ensures you always get accurate code examples and API references for the exact versions of libraries used in this project.

## Why Context7 is Essential for This Project

WitchCityRope uses cutting-edge technologies that evolve rapidly:
- **.NET 9** - The latest version with new features and APIs
- **Blazor Server** - Constantly evolving component model
- **Entity Framework Core 9** - New query capabilities and performance improvements
- **ASP.NET Core Identity** - Security updates and new authentication features
- **Syncfusion Blazor Components** - Regular updates with new components and features

Without Context7, AI assistants might generate code using outdated APIs or patterns that no longer work in the current versions.

## Installation

1. **Quick Install** (if you have npm):
   ```bash
   ./install-context7.sh
   ```

2. **Manual Install**:
   ```bash
   npm install -g @upstash/context7-mcp@latest
   ```

3. **Configure Claude Desktop**:
   - The installation script automatically configures Claude Desktop
   - Configuration is stored in `~/.config/Claude/claude_desktop_config.json`

## How to Use Context7

Simply add "use context7" to any prompt where you need current documentation. Context7 will automatically:
1. Detect the libraries mentioned in your prompt
2. Fetch the latest official documentation
3. Inject relevant code examples into the context
4. Provide version-specific guidance

## Examples for WitchCityRope Development

### ASP.NET Core Identity
```
"How do I implement two-factor authentication with ASP.NET Core Identity in .NET 9? use context7"
```

### Entity Framework Core
```
"Show me the proper way to configure a many-to-many relationship with a payload in EF Core 9 use context7"
```

### Blazor Server Components
```
"What's the correct lifecycle for Blazor Server components in .NET 9? use context7"
```

### Syncfusion Components
```
"How do I implement server-side paging with SfGrid in Syncfusion Blazor? use context7"
```

### PostgreSQL with EF Core
```
"Show me how to use PostgreSQL-specific features like JSONB with EF Core 9 use context7"
```

### ASP.NET Core Middleware
```
"How do I create custom middleware for request logging in ASP.NET Core 9? use context7"
```

### Minimal APIs
```
"Show me best practices for organizing minimal APIs in .NET 9 use context7"
```

## Project-Specific Use Cases

### 1. Authentication & Authorization
```
"How do I implement custom claims-based authorization policies in ASP.NET Core 9? use context7"
```
This ensures you get the latest patterns for the project's role-based access control.

### 2. Blazor Form Validation
```
"Show me how to create custom validation components in Blazor Server .NET 9 use context7"
```
Essential for maintaining the WCR validation component pattern.

### 3. SignalR Configuration
```
"What are the optimal SignalR settings for Blazor Server in production? use context7"
```
Get current best practices for the project's real-time features.

### 4. Database Migrations
```
"How do I handle complex migrations with data transformation in EF Core 9? use context7"
```
Crucial for the ongoing PostgreSQL migration.

### 5. Performance Optimization
```
"What are the latest performance optimization techniques for Blazor Server in .NET 9? use context7"
```
Stay updated with the latest performance improvements.

## Best Practices

1. **Always use Context7 when**:
   - Working with new features you haven't used before
   - Upgrading libraries or frameworks
   - Implementing security-sensitive code
   - Following official Microsoft patterns

2. **Be specific about versions**:
   - "ASP.NET Core 9" instead of just "ASP.NET Core"
   - "EF Core 9 with PostgreSQL" for database-specific features

3. **Combine with project context**:
   - "How do I implement this pattern we use in CLAUDE.md with EF Core 9? use context7"

4. **Use for troubleshooting**:
   - "Why might this EF Core 9 query throw this exception? use context7"

## Integration with Other MCP Servers

Context7 works seamlessly with other MCP servers:

1. **With FileSystem MCP**:
   - Read existing code, then ask: "How can I modernize this to .NET 9 patterns? use context7"

2. **With Memory MCP**:
   - Store architectural decisions with current documentation context

3. **With Docker MCP**:
   - "What's the recommended Docker configuration for .NET 9 apps? use context7"

## Troubleshooting

### Context7 Not Working?

1. **Check installation**:
   ```bash
   npx -y @upstash/context7-mcp@latest --version
   ```

2. **Verify Claude configuration**:
   ```bash
   cat ~/.config/Claude/claude_desktop_config.json
   ```

3. **Test directly**:
   ```bash
   npx -y @upstash/context7-mcp@latest resolve-library-id --name "blazor"
   ```

### Common Issues

1. **"Context7 not found"**: Restart Claude Desktop after installation
2. **Timeout errors**: Check internet connection
3. **Old documentation**: Clear npm cache: `npm cache clean --force`

## Supported Libraries

Context7 supports all major .NET and web development libraries:
- ASP.NET Core
- Entity Framework Core
- Blazor (Server & WebAssembly)
- SignalR
- MediatR
- FluentValidation
- Serilog
- AutoMapper
- Dapper
- And many more...

## Future Claude Sessions

**IMPORTANT**: Future Claude Code sessions should always use Context7 when:
1. Implementing new features with unfamiliar APIs
2. Upgrading any NuGet packages
3. Following Microsoft's official guidance
4. Troubleshooting version-specific issues
5. Ensuring code matches current best practices

Add this to your prompts: "use context7" whenever you need accurate, up-to-date documentation.

## Summary

Context7 eliminates the guesswork and ensures every line of code generated is compatible with the exact versions used in WitchCityRope. It's not just a nice-to-have—it's essential for maintaining code quality and avoiding version-related bugs.