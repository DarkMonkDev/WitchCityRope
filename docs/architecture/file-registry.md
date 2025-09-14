# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-09-14 | `/test-results/paypal-integration-test-2025-09-14.md` | CREATED | PayPal integration test results and validation report - mock service testing, configuration verification, compilation status for local development environment | Test Executor PayPal Validation | ACTIVE | Review after 30 days |
| 2025-09-14 | `/test-results/paypal-test-execution-2025-09-14.json` | CREATED | Structured JSON report of PayPal test execution results - test status, environment validation, service configuration for automated processing | Test Executor PayPal Validation | ACTIVE | Review after 30 days |
| 2025-09-14 | `/tests/unit/api/Services/MockPayPalServiceTests.cs` | CREATED | Unit tests for MockPayPalService covering all interface methods - order creation, capture, refunds, webhooks with comprehensive assertions | Test Executor PayPal Testing | ACTIVE | Keep permanent |
| 2025-09-14 | `/scripts/cloudflare-tunnel-autostart.sh` | CREATED | Auto-start script for Cloudflare tunnel - ensures tunnel runs automatically on terminal open, provides permanent dev URL for PayPal webhooks | PayPal Webhook Configuration | ACTIVE | Keep permanent |
| 2025-09-14 | `/docs/guides-setup/cloudflare-tunnel-setup.md` | CREATED | Comprehensive Cloudflare Tunnel setup documentation - installation, configuration, auto-start setup, troubleshooting for team members | PayPal Webhook Configuration | ACTIVE | Keep permanent |
| 2025-09-14 | `/docs/guides-setup/environment-testing-configuration.md` | CREATED | Environment-specific testing configuration - CI/CD mocking strategy, staging real sandbox, production live setup, mock service implementation | PayPal Testing Strategy | ACTIVE | Keep permanent |
| 2025-09-14 | `/docs/guides-setup/paypal-webhook-setup.md` | MODIFIED | Updated with Cloudflare tunnel instead of ngrok, added all three webhook IDs, corrected port to 5655 | PayPal Webhook Configuration | ACTIVE | Keep permanent |
| 2025-09-14 | `~/.witchcityrope-tunnel-autostart.sh` | CREATED | User home directory auto-start script - launches Cloudflare tunnel automatically on terminal open | PayPal Webhook Configuration | ACTIVE | Keep permanent |
| 2025-09-14 | `~/.bashrc` | MODIFIED | Added auto-start script execution for Cloudflare tunnel on terminal open | PayPal Webhook Configuration | ACTIVE | Keep permanent |
| 2025-09-14 | `/apps/api/Features/Payments/Services/MockPayPalService.cs` | CREATED | Mock PayPal service implementation for CI/CD testing - returns predictable test data without external API calls | PayPal Testing Implementation | ACTIVE | Keep permanent |
| 2025-09-14 | `/apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs` | MODIFIED | Added conditional PayPal service registration based on USE_MOCK_PAYMENT_SERVICE flag | PayPal Testing Implementation | ACTIVE | Keep permanent |
| 2025-09-14 | `/apps/api/Program.cs` | MODIFIED | Added environment validation and mock service configuration support | PayPal Testing Implementation | ACTIVE | Keep permanent |
| 2025-09-14 | `/.env.test` | CREATED | Test environment configuration for CI/CD with mock services enabled | PayPal Testing Implementation | ACTIVE | Keep permanent |
| 2025-09-14 | `/.github/workflows/test.yml` | MODIFIED | Updated to use PostgreSQL, mock services, and test environment configuration | PayPal Testing Implementation | ACTIVE | Keep permanent |
| 2025-09-14 | `/tests/fixtures/paypal-webhooks/payment-completed.json` | CREATED | Test fixture for payment completed webhook testing | PayPal Testing Implementation | ACTIVE | Keep permanent |
| 2025-09-14 | `/tests/fixtures/paypal-webhooks/payment-refunded.json` | CREATED | Test fixture for payment refunded webhook testing | PayPal Testing Implementation | ACTIVE | Keep permanent |
| 2025-09-14 | `/tests/e2e/payment.spec.ts` | CREATED | Playwright E2E tests with CI environment detection and PayPal mocking | PayPal Testing Implementation | ACTIVE | Keep permanent |
| 2025-09-14 | `/home/chad/repos/witchcityrope-react/PROGRESS.md` | MODIFIED | MILESTONE DOCUMENTATION: Added September 14, 2025 milestone - React App Fully Functional breakthrough, zero TypeScript errors, login working, events loading | Librarian Agent Milestone Documentation | ACTIVE | Keep permanent |
| 2025-09-14 | `/home/chad/repos/witchcityrope-react/ARCHITECTURE.md` | MODIFIED | Updated port configuration (5174), added milestone achievement section documenting React migration success | Librarian Agent Milestone Documentation | ACTIVE | Keep permanent |
| 2025-09-14 | `/home/chad/repos/witchcityrope-react/docs/architecture/functional-area-master-index.md` | MODIFIED | Added prominent milestone section documenting React App breakthrough - functional status, technical achievements, development readiness | Librarian Agent Milestone Documentation | ACTIVE | Keep permanent |
| 2025-09-14 | `/home/chad/repos/witchcityrope-react/README.md` | MODIFIED | Updated project status to reflect React milestone achievement, added quick start section, documented breakthrough significance | Librarian Agent Milestone Documentation | ACTIVE | Keep permanent |
| 2025-09-14 | `/home/chad/repos/witchcityrope-react/docs/lessons-learned/librarian-lessons-learned.md` | MODIFIED | Added milestone documentation pattern with systematic process for capturing major achievements across all key project files | Librarian Agent Process Documentation | ACTIVE | Keep permanent |
| 2025-09-14 | `/home/chad/repos/witchcityrope-react/session-work/2025-09-14/react-milestone-achievement-2025-09-14.md` | CREATED | Comprehensive milestone summary documenting React App breakthrough - technical details, impact analysis, development readiness confirmation | Librarian Agent Milestone Summary | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/design/payment-system-database-design.md` | CREATED | Comprehensive database schema design for Payment System - sliding scale pricing, PCI compliance, Stripe integration, audit trails, PostgreSQL optimization, Entity Framework Core configuration supporting community values with security and performance | Database Designer Agent Payment System | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/requirements/payment-system-business-requirements.md` | CREATED | Comprehensive business requirements for Payment system extraction from legacy to modern API - community values (sliding scale), security, user stories, MVP scope, PCI compliance | Business Requirements Agent Payment System | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/implementation/architecture-decisions/payment-service-structure.md` | CREATED | Architecture decision record for Payment service structure - monolithic vs microservices approach for MVP implementation | Business Requirements Agent Payment System | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/implementation/architecture-decisions/sliding-scale-implementation.md` | CREATED | Architecture decision record for sliding scale implementation - storage strategy, calculation method, security considerations | Business Requirements Agent Payment System | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/implementation/architecture-decisions/stripe-integration-approach.md` | CREATED | Architecture decision record for Stripe integration approach - SDK vs REST API, error handling, webhook security | Business Requirements Agent Payment System | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/implementation/architecture-decisions/payment-audit-strategy.md` | CREATED | Architecture decision record for payment audit strategy - comprehensive logging, security events, compliance tracking | Business Requirements Agent Payment System | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/lessons-learned/backend-lessons-learned.md` | CREATED | Compilation issue solutions and backend development patterns for project architecture migration | Backend Developer Session | ACTIVE | Keep permanent |
| 2025-09-12 | `/apps/api/Migrations/20250912040532_AddEvents.cs` | CREATED | Entity Framework migration adding Events table with session support | Backend Developer Session | ACTIVE | Keep permanent |
| 2025-09-12 | `/apps/api/Migrations/20250912040532_AddEvents.Designer.cs` | CREATED | Entity Framework migration designer file for Events table | Backend Developer Session | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-events-endpoint/design/events-api-database-schema.md` | CREATED | Database schema design for Events API with sessions, tickets, and performance optimization | Database Designer Session | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-events-endpoint/requirements/events-api-requirements.md` | CREATED | Business requirements document for Events API with admin management and public display features | Business Requirements Session | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-events-endpoint/implementation/events-endpoint-implementation-plan.md` | CREATED | Implementation plan for Events API endpoint with minimal viable product approach | Backend Developer Session | ACTIVE | Keep permanent |
| 2025-09-12 | `/session-work/2025-09-12/compilation-success-verification.md` | CREATED | Verification document confirming clean compilation of entire solution | Backend Developer Session | TEMPORARY | Delete after 30 days |
| 2025-09-11 | `/home/chad/repos/witchcityrope-react/docs/lessons-learned/test-executor-lessons-learned.md` | MODIFIED | Updated with compilation check requirements and React app diagnostic patterns | Test Executor Session | ACTIVE | Keep permanent |
| 2025-09-11 | `/home/chad/repos/witchcityrope-react/docs/lessons-learned/react-lessons-learned.md` | CREATED | React development lessons including component patterns and state management | React Developer Session | ACTIVE | Keep permanent |
| 2025-09-11 | `/home/chad/repos/witchcityrope-react/docs/architecture/decisions.md` | CREATED | Architecture decision records for technical choices and patterns | Architecture Documentation | ACTIVE | Keep permanent |
| 2025-09-11 | `/home/chad/repos/witchcityrope-react/test-results/comprehensive-test-report-2025-09-11.json` | CREATED | Test execution results from comprehensive testing session | Test Executor Session | TEMPORARY | Delete after 30 days |
| 2025-09-11 | `/home/chad/repos/witchcityrope-react/test-results/execution-summary-2025-09-11.md` | CREATED | Summary of test execution and quality metrics | Test Executor Session | TEMPORARY | Delete after 30 days |
| 2025-09-10 | `/home/chad/repos/witchcityrope-react/.env.development` | CREATED | Development environment configuration with database and API settings | Environment Setup | ACTIVE | Keep permanent |
| 2025-09-10 | `/home/chad/repos/witchcityrope-react/docker-compose.dev.yml` | CREATED | Docker Compose override for development environment | Docker Configuration | ACTIVE | Keep permanent |
| 2025-09-10 | `/home/chad/repos/witchcityrope-react/dev.sh` | CREATED | Development utility script for Docker management | Development Tools | ACTIVE | Keep permanent |
| 2025-08-15 | `/home/chad/repos/witchcityrope-react/docs/development/development-history-2025-07.md` | CREATED | Archive of July 2025 development sessions (extracted from PROGRESS.md during maintenance) | Progress Maintenance | ARCHIVED | Keep as historical record |
| 2025-08-15 | `/home/chad/repos/witchcityrope-react/docs/architecture/react-migration/architecture-decisions.md` | CREATED | Architecture decisions for React migration from Blazor Server | React Migration Documentation | ACTIVE | Keep permanent |

## Cleanup Schedule

### Monthly Review (First Week of Month)
- Review TEMPORARY files older than 30 days
- Archive completed session work to development history
- Update status of ACTIVE files if needed

### Quarterly Deep Clean (March, June, September, December)
- Review all files for continued relevance
- Move outdated ACTIVE files to ARCHIVED status
- Update cleanup dates for permanent files
- Verify all links and references work correctly

### File Status Definitions
- **ACTIVE**: Currently relevant and in use
- **ARCHIVED**: Historical value but not actively used
- **TEMPORARY**: Short-term files to be reviewed for cleanup