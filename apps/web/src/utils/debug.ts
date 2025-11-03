/**
 * Debug logging utilities
 * Logs only show in development mode to keep production console clean
 */

const isDevelopment = import.meta.env.DEV;

export const debugLog = (...args: any[]) => {
  if (isDevelopment) {
    console.log(...args);
  }
};

export const debugWarn = (...args: any[]) => {
  if (isDevelopment) {
    console.warn(...args);
  }
};

export const debugError = (...args: any[]) => {
  // Errors always show, even in production
  console.error(...args);
};

export const debugInfo = (...args: any[]) => {
  if (isDevelopment) {
    console.info(...args);
  }
};
