# Context7 Quick Reference

## 🚀 Quick Usage
Just add "use context7" to any prompt needing documentation!

## 📚 Common Examples for WitchCityRope

### ASP.NET Core Identity
```
"How to implement custom user claims in ASP.NET Core Identity 9? use context7"
```

### Blazor Server
```
"Show me Blazor Server component lifecycle in .NET 10 use context7"
```

### Entity Framework Core
```
"How to configure value objects in EF Core 9? use context7"
```

### Syncfusion Blazor
```
"SfGrid server-side operations in Blazor use context7"
```

### PostgreSQL + EF Core
```
"PostgreSQL-specific features in EF Core 9 use context7"
```

## 💡 When to Use

✅ **Always use for:**
- New feature implementation
- Library-specific questions
- Version-specific APIs
- Best practices
- Troubleshooting errors

❌ **Don't use for:**
- Project-specific code (use FileSystem MCP)
- General programming concepts
- Non-library questions

## 🎯 Pro Tips

1. **Be specific about versions**:
   - ✅ "ASP.NET Core 9"
   - ❌ "ASP.NET Core"

2. **Combine with project context**:
   - "How to implement this pattern from CLAUDE.md in EF Core 9? use context7"

3. **Use for migrations**:
   - "Migrating from EF Core 8 to 9 breaking changes use context7"

## 🔧 Troubleshooting

**Not working?**
1. Restart Claude Desktop
2. Check: `npx -y @upstash/context7-mcp@latest --version`
3. Verify config: `~/.config/Claude/claude_desktop_config.json`

## 📖 Full Guide
See: `/docs/CONTEXT7_USAGE_GUIDE.md`