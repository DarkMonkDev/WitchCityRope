// types/react-query.d.ts
declare module '@tanstack/react-query' {
  interface Register {
    defaultError: ApiError
  }
}

interface ApiError {
  message: string
  status: number
  code?: string
  details?: Record<string, string[]>
}