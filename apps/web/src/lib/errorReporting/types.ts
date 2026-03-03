export interface ClientErrorReport {
  message: string;
  stack?: string;
  type: 'unhandled_error' | 'unhandled_rejection' | 'react_error' | 'api_error';
  url: string;
  timestamp: string;
  userAgent: string;
  componentStack?: string;
  metadata?: Record<string, unknown>;
}

export interface ClientErrorBatch {
  errors: ClientErrorReport[];
}
