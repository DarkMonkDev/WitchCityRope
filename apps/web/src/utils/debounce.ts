// Simple debounce utility for form validation
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  delay: number
): T & { cancel: () => void; flush: () => void } {
  let timeoutId: NodeJS.Timeout | null = null;
  let lastArgs: Parameters<T>;
  let lastResult: ReturnType<T>;

  const debounced = ((...args: Parameters<T>): ReturnType<T> => {
    lastArgs = args;
    
    if (timeoutId !== null) {
      clearTimeout(timeoutId);
    }

    timeoutId = setTimeout(() => {
      lastResult = func(...args);
      timeoutId = null;
    }, delay);

    return lastResult;
  }) as T & { cancel: () => void; flush: () => void };

  debounced.cancel = () => {
    if (timeoutId !== null) {
      clearTimeout(timeoutId);
      timeoutId = null;
    }
  };

  debounced.flush = () => {
    if (timeoutId !== null) {
      clearTimeout(timeoutId);
      lastResult = func(...lastArgs);
      timeoutId = null;
    }
    return lastResult;
  };

  return debounced;
}