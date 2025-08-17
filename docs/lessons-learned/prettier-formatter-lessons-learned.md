# Prettier Formatter Lessons Learned

## Formatting Standards and Configurations

### Project Configuration (.prettierrc)
- **Semi**: false (no semicolons)
- **Single Quote**: true (use single quotes)
- **Print Width**: 100 characters
- **Tab Width**: 2 spaces
- **Trailing Comma**: "es5"
- **Arrow Parens**: "always"
- **End of Line**: "lf"

### File Types Supported
- TypeScript/React: .ts, .tsx files
- JavaScript: .js, .jsx files
- Test files: .spec.ts, .test.tsx
- JSON and markdown files

## Common Formatting Issues and Solutions

### Issue: Files Need Formatting After Development
**Solution**: Always run `npx prettier --write` on modified files before commits
**Pattern**: Use absolute paths when formatting specific files

### Issue: Missing Newlines at End of Files
**Solution**: Prettier automatically adds final newlines per endOfLine: "lf" config
**Action**: Let Prettier handle this automatically

### Issue: Mixed Indentation
**Solution**: Project uses 2-space indentation consistently
**Enforcement**: tabWidth: 2, useTabs: false in config

## Tool Integration Knowledge

### Command Patterns That Work
```bash
# Check formatting status
npx prettier --check "path/to/file.tsx"

# Format specific files
npx prettier --write "/absolute/path/to/file.tsx"

# Format multiple files with absolute paths
npx prettier --write "/path/file1.tsx" "/path/file2.tsx"
```

### Multi-Language Formatting
- **TypeScript/React**: Use Prettier for all .ts/.tsx files
- **C#**: Use `dotnet format` for .cs files (works well out of box)
- **Integration**: Both tools work independently without conflicts

### Working Directory Considerations
- Run Prettier from repository root where .prettierrc exists
- Use absolute paths when running from subdirectories
- Relative paths can fail when not in correct working directory

## Best Practices

### Phase 5 Quality Gate Process
1. Check current formatting status with `--check` flag first
2. Format all TypeScript/React files with `--write`
3. Format all C# files with `dotnet format`
4. Verify formatting success with final `--check`
5. Document which files were modified in formatting report

### File Organization for Formatting
- React components: apps/web/src/components/*.tsx
- Pages: apps/web/src/pages/*.tsx
- Types: apps/web/src/types/*.ts
- Tests: tests/e2e/*.spec.ts and apps/web/src/components/__tests__/*.test.tsx
- API: apps/api/**/*.cs files

### Automation Opportunities
- Consider pre-commit hooks for automatic formatting
- IDE integration with format-on-save for development
- CI/CD formatting checks to prevent unformatted code

---
*Last updated: 2025-08-16 during Phase 5 formatting of vertical slice implementation*
