# Lint Validator Lessons Learned

## Critical Rules and Patterns

### TypeScript ESLint Configuration Issues
- **Monorepo Structure**: Root-level ESLint config requires TypeScript ESLint dependencies to be installed locally, even when packages have them
- **Workspace Isolation**: ESLint from workspace packages cannot be used for files outside the workspace scope
- **Test Setup Issues**: Vitest test setup files need explicit `vi` import from vitest to avoid TypeScript errors

### Turbo Monorepo Linting
- **Turbo Integration**: Use `npm run lint` from root to run all workspace lints via Turbo
- **Cache Benefits**: Turbo caches lint results for unchanged files, improving performance
- **Workspace Scope**: ESLint only works within configured workspace directories

## Common Issues and Solutions

### TypeScript Errors
1. **Missing 'vi' in test setup**: Add `import { vi } from "vitest";` to test setup files
2. **Any type warnings**: Replace `any` with specific types or structured interfaces
3. **Compilation failures**: Use `npx tsc --noEmit` to check types without building

### C# Formatting Issues
- **dotnet format**: Always run `dotnet format` to fix whitespace and formatting issues
- **Verification**: Use `dotnet format --verify-no-changes` to check formatting compliance
- **Common Fixes**: Indentation, line endings, and spacing issues are automatically corrected

### ESLint Configuration
- **Max Warnings**: Web app configured with `--max-warnings 0` requiring zero warnings to pass
- **File Extensions**: Use `--ext .ts,.tsx` for TypeScript files
- **JSON Output**: Use `--format=json` for programmatic analysis of lint results

## Tool-Specific Knowledge

### ESLint
- **Version**: 8.57.1 installed in web workspace
- **Config**: Modern flat config in `eslint.config.js` using typescript-eslint v8
- **Rules**: Configured for React, TypeScript, and Prettier integration

### TypeScript
- **Version**: 5.9.2
- **Config**: Strict mode enabled with `noUnusedLocals` and `noUnusedParameters`
- **Project References**: Uses project references for monorepo structure

### dotnet format
- **Auto-fix**: Automatically corrects whitespace, indentation, and style issues
- **Verification**: Can verify compliance without making changes
- **Scope**: Works on entire project or specific files

## Best Practices

### Validation Workflow
1. Run TypeScript compilation check first (`tsc --noEmit`)
2. Run ESLint on specific files or use workspace scripts
3. Run C# formatting validation and auto-fix
4. Verify all tools report success before proceeding

### Performance Optimization
- Use Turbo for workspace-wide linting with caching
- Limit ESLint to specific files when debugging issues
- Use JSON output for automated parsing of results

### Quality Gates
- Zero TypeScript compilation errors required
- Zero ESLint errors/warnings required (strict mode)
- All C# files must pass formatting verification
- Test files included in validation scope

---
*This file is maintained by the lint-validator agent. Updated during vertical slice validation - 2025-08-16*
