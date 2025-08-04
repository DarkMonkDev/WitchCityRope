# Context7 MCP Server Configuration

## What is Context7?

Context7 is an MCP (Model Context Protocol) server that provides real-time, version-specific documentation for libraries and frameworks. When you add "use context7" to your prompts, it automatically fetches the latest official documentation and injects it into Claude's context.

## Why Context7 for WitchCityRope?

This project uses:
- .NET 9 (latest version)
- Blazor Server
- Entity Framework Core 9
- ASP.NET Core Identity
- Syncfusion Blazor Components

Without Context7, Claude might generate outdated code or use deprecated APIs. Context7 ensures all generated code matches your exact library versions.

## Configuration

### For Claude Desktop

The configuration is in `.claude/mcp-config.json`:

```json
{
  "mcpServers": {
    "context7": {
      "command": "npx",
      "args": ["-y", "@upstash/context7-mcp@latest"],
      "description": "Context7 provides up-to-date, version-specific documentation for libraries and frameworks",
      "autoApprove": ["resolve-library-id", "get-library-docs"],
      "timeout": 60
    }
  }
}
```

### Global Installation

To install Context7 globally for all Claude sessions:

1. Create/edit `~/.config/Claude/claude_desktop_config.json`
2. Add the Context7 configuration
3. Restart Claude Desktop

## Usage Examples

### Basic Usage
Add "use context7" to any prompt:
```
"How do I configure ASP.NET Core Identity in .NET 9? use context7"
```

### Project-Specific Examples

#### Blazor Components
```
"Show me how to implement a custom Blazor validation component use context7"
```

#### Entity Framework
```
"Explain owned entities in EF Core 9 with PostgreSQL use context7"
```

#### Syncfusion
```
"How to use SfGrid with server-side paging in Blazor? use context7"
```

## Benefits

1. **Accuracy**: Always get code for .NET 9, not older versions
2. **Completeness**: Full API documentation, not just snippets
3. **Currency**: Documentation is fetched in real-time
4. **Relevance**: Examples match your exact use case

## Troubleshooting

If Context7 doesn't work:

1. Check Node.js version (requires 18+):
   ```bash
   node -v
   ```

2. Test Context7 directly:
   ```bash
   npx -y @upstash/context7-mcp@latest --version
   ```

3. Verify configuration in Claude Desktop settings

## Best Practices for WitchCityRope

1. **Always use Context7 for**:
   - New feature implementation
   - Library upgrades
   - Security-related code
   - Performance optimization

2. **Specify versions**:
   - ".NET 9" not just ".NET"
   - "EF Core 9" not just "Entity Framework"

3. **Combine with project knowledge**:
   - Reference CLAUDE.md patterns
   - Use with existing code examples

## For Future Sessions

**IMPORTANT**: All future Claude Code sessions working on this project should use Context7 when dealing with:
- Framework APIs
- Library integrations
- Version-specific features
- Best practices and patterns

Simply add "use context7" to relevant prompts to ensure accuracy.