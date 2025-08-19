# Lint Validator Agent - Lessons Learned

## Monorepo ESLint Dependencies Must Be Installed at Root Level

**Date**: 2025-08-16
**Category**: Configuration
**Severity**: Critical

### Context
During monorepo setup with Turbo, ESLint was configured at the workspace level but failing when running from root. Even though workspace packages had ESLint and TypeScript ESLint dependencies, the root-level ESLint configuration couldn't access them.

### What We Learned
Root-level ESLint configurations require their own installation of TypeScript ESLint dependencies, even when workspace packages already have them. Workspace isolation prevents sharing of these dependencies for files outside workspace scope.

### Action Items
- [ ] Always install @typescript-eslint/eslint-plugin and @typescript-eslint/parser at root level for monorepos
- [ ] Verify ESLint dependencies exist at the appropriate level for the configuration scope
- [ ] Document dependency requirements in monorepo setup guides

### Impact
Prevents ESLint configuration failures in monorepo setups and ensures consistent linting across all project files.

### Tags
#critical #configuration #monorepo #eslint #dependencies

---

## Vitest Test Setup Files Require Explicit vi Import

**Date**: 2025-08-16
**Category**: Testing
**Severity**: High

### Context
When configuring Vitest test setup files, TypeScript errors occurred because the global `vi` object was not recognized, even though Vitest was properly configured and tests were running.

### What We Learned
Vitest test setup files need explicit import of the `vi` object from vitest to avoid TypeScript errors, even when `vi` is available globally during test execution.

### Action Items
- [ ] Add `import { vi } from "vitest";` to all Vitest test setup files
- [ ] Update TypeScript configuration templates to include this pattern
- [ ] Document this requirement in testing guidelines

### Impact
Eliminates TypeScript errors in test setup files and ensures proper type checking for Vitest globals.

### Tags
#testing #vitest #typescript #setup #imports

---

## Turbo Provides Optimal Linting Performance Through Caching

**Date**: 2025-08-16
**Category**: Performance
**Severity**: Medium

### Context
Monorepo linting was slow when running ESLint individually on each workspace. Turbo was configured but not being used optimally for linting tasks.

### What We Learned
Using `npm run lint` from root with Turbo integration provides significant performance benefits through intelligent caching of lint results for unchanged files. This dramatically reduces validation time in large monorepos.

### Action Items
- [ ] Always use Turbo-orchestrated lint commands for workspace-wide validation
- [ ] Configure lint scripts to leverage Turbo caching
- [ ] Document performance benefits in validation workflows

### Impact
Reduces lint validation time from minutes to seconds for unchanged files, improving developer productivity.

### Tags
#performance #turbo #monorepo #caching #workflow

---

## ESLint Max Warnings Zero Enforces Strict Quality Standards

**Date**: 2025-08-16
**Category**: Configuration
**Severity**: Medium

### Context
Web application was configured with `--max-warnings 0` flag, requiring zero ESLint warnings to pass validation. This enforces stricter code quality standards than allowing warnings.

### What We Learned
Configuring ESLint with `--max-warnings 0` creates a strict quality gate that treats warnings as failures, preventing technical debt accumulation and enforcing consistent code standards.

### Action Items
- [ ] Document the strict warning policy in coding standards
- [ ] Ensure all team members understand zero-warning requirement
- [ ] Consider this configuration for all new projects

### Impact
Maintains high code quality by preventing warning accumulation and enforcing immediate resolution of code quality issues.

### Tags
#configuration #quality #eslint #standards #warnings

---

## TypeScript Compilation Check Should Precede ESLint Validation

**Date**: 2025-08-16
**Category**: Workflow
**Severity**: Medium

### Context
During validation workflow, running ESLint before TypeScript compilation could miss type-related issues that would prevent successful builds.

### What We Learned
TypeScript compilation checks (`tsc --noEmit`) should be performed before ESLint validation to catch fundamental type issues early. ESLint may pass on code that won't compile.

### Action Items
- [ ] Always run `tsc --noEmit` before ESLint in validation workflows
- [ ] Update validation scripts to enforce this order
- [ ] Document the rationale in validation procedures

### Impact
Catches compilation issues early in the validation process, preventing wasted time on code that won't build.

### Tags
#workflow #typescript #validation #process #compilation

---

## C# Formatting Auto-Fix Eliminates Manual Style Corrections

**Date**: 2025-08-16
**Category**: Tooling
**Severity**: Low

### Context
Manual correction of C# formatting issues was time-consuming and error-prone. The `dotnet format` tool was available but not consistently used.

### What We Learned
`dotnet format` automatically corrects whitespace, indentation, and style issues in C# code. Using `--verify-no-changes` can check compliance without making modifications.

### Action Items
- [ ] Include `dotnet format` in automated validation workflows
- [ ] Use `--verify-no-changes` for CI/CD verification
- [ ] Document C# formatting requirements and auto-fix capabilities

### Impact
Eliminates manual formatting work and ensures consistent C# code style across the project.

### Tags
#tooling #csharp #formatting #automation #style

