import type { ClientErrorReport } from './types';
import { sanitizeErrorData } from './sanitizer';

const MAX_QUEUE_SIZE = 50;
const BATCH_SIZE = 10;
const DEBOUNCE_MS = 2000;
const DEDUP_WINDOW_MS = 60_000;

let queue: ClientErrorReport[] = [];
let recentHashes = new Map<string, number>();
let flushTimer: ReturnType<typeof setTimeout> | null = null;

function hashError(message: string, stack?: string): string {
  return `${message.slice(0, 100)}::${(stack ?? '').slice(0, 200)}`;
}

function getCsrfToken(): string | null {
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
}

function pruneRecentHashes(): void {
  const now = Date.now();
  for (const [hash, timestamp] of recentHashes) {
    if (now - timestamp > DEDUP_WINDOW_MS) {
      recentHashes.delete(hash);
    }
  }
}

async function sendBatch(errors: ClientErrorReport[]): Promise<void> {
  try {
    const csrfToken = getCsrfToken();
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (csrfToken) headers['X-CSRF-TOKEN'] = csrfToken;

    await fetch('/api/client-errors', {
      method: 'POST',
      headers,
      body: JSON.stringify({ errors }),
      keepalive: true,
    });
  } catch {
    // Silently fail - never block the UI for error reporting
  }
}

function scheduleFlush(): void {
  if (flushTimer !== null) return;
  flushTimer = setTimeout(() => {
    flushTimer = null;
    void drainQueue();
  }, DEBOUNCE_MS);
}

async function drainQueue(): Promise<void> {
  while (queue.length > 0) {
    const batch = queue.splice(0, BATCH_SIZE);
    await sendBatch(batch);
  }
}

export function reportError(report: ClientErrorReport): void {
  try {
    const sanitized = sanitizeErrorData(report);

    const hash = hashError(sanitized.message, sanitized.stack);
    pruneRecentHashes();
    if (recentHashes.has(hash)) return;
    recentHashes.set(hash, Date.now());

    queue.push(sanitized);
    if (queue.length > MAX_QUEUE_SIZE) {
      queue = queue.slice(queue.length - MAX_QUEUE_SIZE);
    }

    scheduleFlush();
  } catch {
    // Never throw from error reporting
  }
}

export async function flushErrors(): Promise<void> {
  try {
    if (flushTimer !== null) {
      clearTimeout(flushTimer);
      flushTimer = null;
    }
    await drainQueue();
  } catch {
    // Never throw from error reporting
  }
}
