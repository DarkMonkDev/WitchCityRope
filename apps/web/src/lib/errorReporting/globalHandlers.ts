import { reportError, flushErrors } from './errorReportingService';

export function initializeErrorReporting(): void {
  window.onerror = (message, source, lineno, colno, error) => {
    reportError({
      message: typeof message === 'string' ? message : 'Unknown error',
      stack: error?.stack,
      type: 'unhandled_error',
      url: window.location.href,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      metadata: { source, lineno, colno },
    });
  };

  window.onunhandledrejection = (event: PromiseRejectionEvent) => {
    const error = event.reason;
    reportError({
      message: error?.message ?? String(error),
      stack: error?.stack,
      type: 'unhandled_rejection',
      url: window.location.href,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
    });
  };

  window.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      void flushErrors();
    }
  });
}
