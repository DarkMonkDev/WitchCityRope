// Temporary type declarations for @tanstack/react-query
// This file provides missing type exports until we resolve the package type resolution issue

declare module '@tanstack/react-query' {
  import React from 'react';

  // Basic types needed for React Query
  export interface QueryClient {
    new (config?: any): QueryClient;
    getQueryData(queryKey: any): any;
    setQueryData(queryKey: any, data: any): void;
    invalidateQueries(options?: any): Promise<void>;
    refetchQueries(options?: any): Promise<void>;
    clear(): void;
    removeQueries(options?: any): void;
    cancelQueries<TData = unknown>(options?: any): Promise<void>;
  }

  export interface UseQueryResult<TData = unknown, TError = Error> {
    data: TData | undefined;
    error: TError | null;
    isLoading: boolean;
    isError: boolean;
    isSuccess: boolean;
    refetch: () => Promise<any>;
  }

  export interface UseMutationResult<TData = unknown, TError = Error, TVariables = unknown> {
    mutate: (variables: TVariables) => void;
    mutateAsync: (variables: TVariables) => Promise<TData>;
    data: TData | undefined;
    error: TError | null;
    isLoading: boolean;
    isError: boolean;
    isSuccess: boolean;
    isPending: boolean;
  }

  export interface UseInfiniteQueryResult<TData = unknown, TError = Error> {
    data: { pages: TData[]; pageParams: any[] } | undefined;
    error: TError | null;
    isLoading: boolean;
    isError: boolean;
    isSuccess: boolean;
    fetchNextPage: () => Promise<any>;
    hasNextPage: boolean;
    isFetchingNextPage: boolean;
  }

  // Hook declarations
  export function useQuery<TData = unknown, TError = Error>(
    options: any
  ): UseQueryResult<TData, TError>;

  export function useMutation<TData = unknown, TError = Error, TVariables = unknown>(
    options: any
  ): UseMutationResult<TData, TError, TVariables>;

  export function useInfiniteQuery<TData = unknown, TError = Error>(
    options: any
  ): UseInfiniteQueryResult<TData, TError>;

  export function useQueryClient(): QueryClient;

  // Component declarations
  export interface QueryClientProviderProps {
    client: QueryClient;
    children: React.ReactNode;
  }

  export const QueryClientProvider: React.FC<QueryClientProviderProps>;
  
  export interface QueryErrorResetBoundaryProps {
    children: React.ReactNode;
  }

  export const QueryErrorResetBoundary: React.FC<QueryErrorResetBoundaryProps>;

  // Create QueryClient class
  export const QueryClient: new (config?: any) => QueryClient;
}