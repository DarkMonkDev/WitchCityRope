import type { ClientErrorReport } from './types';

const EMAIL_PATTERN = /[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/g;
const JWT_PATTERN = /[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}/g;
const AUTH_COOKIE_PATTERN = /auth-token=[^;\s]+/g;

function sanitizeString(value: string): string {
  return value
    .replace(EMAIL_PATTERN, '[email]')
    .replace(JWT_PATTERN, '[token]')
    .replace(AUTH_COOKIE_PATTERN, 'auth-token=[redacted]');
}

export function sanitizeErrorData(report: ClientErrorReport): ClientErrorReport {
  return {
    ...report,
    message: sanitizeString(report.message),
    stack: report.stack ? sanitizeString(report.stack) : undefined,
    url: sanitizeString(report.url),
    userAgent: report.userAgent,
    componentStack: report.componentStack
      ? sanitizeString(report.componentStack)
      : undefined,
  };
}
