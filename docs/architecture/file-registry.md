# File Registry
<!-- Last Updated: 2026-03-17 -->
<!-- Version: 4.556 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Purpose
This registry tracks all files created, modified, and deleted in the WitchCityRope project. It provides accountability and enables cleanup of temporary files.

## Recent Updates

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2026-03-17 | /docs/standards-processes/development-standards/authentication-patterns.md | MODIFIED | Replaced Blazor-era auth patterns with redirect to current auth documentation locations | Librarian: Auth documentation Priority 2 update | ACTIVE | Never |
| 2026-03-17 | /docs/functional-areas/authentication/bff-authentication-implementation-guide.md | MODIFIED | Updated for dual-cookie system, refresh token rotation, RememberMe, 15-min JWT, rate limits, frontend refresh strategy | Librarian: Auth documentation Priority 2 update | ACTIVE | Never |
| 2026-03-17 | /docs/functional-areas/authentication/README.md | MODIFIED | Updated for dual-cookie BFF, refresh token rotation, RememberMe, rate limits, current architecture details | Librarian: Auth documentation Priority 2 update | ACTIVE | Never |
| 2026-03-14 | /docs/functional-areas/deployment/production-deployment-guide.md | MODIFIED | Complete rewrite - removed aspirational content (ELK, Kubernetes, Prometheus, blue-green, etc), replaced with actual DigitalOcean deployment facts | Librarian: Production guide accuracy cleanup | ACTIVE | Never |
| 2026-03-14 | /docs/functional-areas/deployment/dns-cutover-plan.md | CREATED | Go-live DNS cutover plan for switching witchcityrope.com from Wix to DigitalOcean production server | Librarian: DNS cutover documentation | ACTIVE | Never |
| 2026-03-02 | /docs/functional-areas/logging-observability/README.md | CREATED | Comprehensive audit and project plan for site-wide logging infrastructure | E19 - CC failure logging investigation | ACTIVE | N/A |
| 2026-03-02 | /docs/functional-areas/payment-entity-consolidation/README.md | CREATED | Comprehensive audit and project plan for consolidating dual payment entities | E19 - CC failure logging investigation | ACTIVE | N/A |
| 2026-02-23 | /apps/web/src/lib/utils/sanitizeHtml.ts | CREATED | DOMPurify HTML sanitization utility for XSS prevention | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /nginx/production.conf | CREATED | Production-grade nginx config with full security headers, CSP, rate limiting | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /docs/functional-areas/security/audit/security-audit-report-2026-02-23.md | CREATED | Full security audit findings with severity ratings and fix status | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /docs/functional-areas/security/audit/exposed-secrets-inventory-2026-02-23.md | CREATED | Inventory of all secrets found committed in repo for rotation planning | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /apps/api/Program.cs | MODIFIED | Secured Hangfire dashboard, removed hardcoded secrets (fail-fast), fixed CORS (env-aware), strengthened password policy | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs | MODIFIED | Removed unauthenticated debug endpoint exposing JWT/cookie data | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /apps/web/nginx.conf | MODIFIED | Security headers (HSTS, Permissions-Policy), tightened CSP, rate limiting, removed CORS reflection | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /nginx/staging.conf | MODIFIED | Added CSP, Permissions-Policy, fixed orphaned rate limiting blocks | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /deployment/deploy-linux.sh | MODIFIED | Updated to use static nginx configs instead of generating inline | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /apps/web/src/features/cms/components/CmsPage.tsx | MODIFIED | Added DOMPurify sanitization to dangerouslySetInnerHTML | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /apps/web/src/features/cms/components/CmsRevisionCard.tsx | MODIFIED | Added DOMPurify sanitization to dangerouslySetInnerHTML | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /apps/web/src/pages/events/EventDetailPage.tsx | MODIFIED | Added DOMPurify sanitization to 2 dangerouslySetInnerHTML locations | Security Audit Remediation | ACTIVE | Never |
| 2026-02-23 | /apps/web/src/lib/api/client.ts | MODIFIED | Guarded console.debug calls with import.meta.env.DEV | Security Audit Remediation | ACTIVE | Never |
| 2026-02-22 | /docs/functional-areas/payments/research/2026-02-22-acceptjs-react-integration-research.md | CREATED | Comprehensive research document on Authorize.net Accept.js integration for React + TypeScript - Covers Accept.js flow, PCI compliance (SAQ A-EP), react-acceptjs package evaluation, TypeScript types, React component patterns with Mantine UI, .NET backend nonce processing with AuthorizeNet SDK, security considerations, CSP requirements, error handling, implementation guidance | Technology Researcher: Accept.js Research | ACTIVE | Never |
| 2025-12-14 | /docs/functional-areas/member-import/post-import-email-workflow-guide.md | CREATED | Comprehensive guide for post-import email workflow - Documents NewImportedUsers segment, per-user variable replacement ({{user_name}}, {{reset_url}}, {{verification_url}}), step-by-step workflow for sending welcome emails with password reset links after importing vetted members. Includes 8 user segments, troubleshooting, security considerations, performance details, FAQ | Librarian: Document Post-Import Email Workflow | ACTIVE | Never |
