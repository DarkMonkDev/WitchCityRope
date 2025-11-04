# State Management Patterns

**Purpose**: Zustand state management patterns, React Context usage, and state synchronization for WitchCityRope.
**When to Read**: When implementing global state, stores, or state synchronization.
**Related**: [React Patterns](./react-patterns.md), [TypeScript Patterns](./typescript-patterns.md)

## State Management Stack

**Primary**: Zustand (lightweight state management)
**Secondary**: React Context (component-level state)
**API State**: TanStack Query (React Query v5)

## Zustand Store Pattern

### Standard Store Definition
```typescript
// src/stores/auth.ts
import { create } from 'zustand';
import type { components } from '@witchcityrope/shared-types';

type User = components['schemas']['UserDto'];

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;

  // Actions
  setUser: (user: User | null) => void;
  checkAuth: () => Promise<void>;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isLoading: true,

  setUser: (user) =>
    set({
      user,
      isAuthenticated: user !== null,
    }),

  checkAuth: async () => {
    set({ isLoading: true });
    try {
      const response = await fetch('/api/auth/check-auth');
      if (response.ok) {
        const user = await response.json();
        set({ user, isAuthenticated: true });
      } else {
        set({ user: null, isAuthenticated: false });
      }
    } catch (error) {
      set({ user: null, isAuthenticated: false });
    } finally {
      set({ isLoading: false });
    }
  },

  logout: async () => {
    await fetch('/api/auth/logout', { method: 'POST' });
    set({ user: null, isAuthenticated: false });
  },
}));
```

## Zustand Selector Patterns

### Problem: Object Selectors Cause Re-renders
```typescript
// ❌ WRONG: Object selector creates new reference every render
const { user, isAuthenticated } = useAuthStore(state => ({
  user: state.user,
  isAuthenticated: state.isAuthenticated,
}));
```

### Solution: Individual Selectors
```typescript
// ✅ CORRECT: Individual selectors only re-render when value changes
const user = useAuthStore(state => state.user);
const isAuthenticated = useAuthStore(state => state.isAuthenticated);
```

### Solution: Shallow Equality (when needed)
```typescript
import { shallow } from 'zustand/shallow';

// ✅ CORRECT: Shallow equality for multi-value selection
const { user, isAuthenticated } = useAuthStore(
  state => ({ user: state.user, isAuthenticated: state.isAuthenticated }),
  shallow
);
```

## React Query (TanStack Query) Pattern

### API Data Fetching
```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { components } from '@witchcityrope/shared-types';

type EventDto = components['schemas']['EventDto'];

// Query hook
export function useEvent(id: number) {
  return useQuery({
    queryKey: ['event', id],
    queryFn: async () => {
      const response = await fetch(`/api/events/${id}`);
      if (!response.ok) {
        throw new Error('Failed to fetch event');
      }
      return response.json() as Promise<EventDto>;
    },
  });
}

// Mutation hook
export function useCreateEvent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Partial<EventDto>) => {
      const response = await fetch('/api/events', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      return response.json();
    },
    onSuccess: () => {
      // Invalidate and refetch events list
      queryClient.invalidateQueries({ queryKey: ['events'] });
    },
  });
}

// Usage in component
export function EventDetail({ id }: { id: number }) {
  const { data: event, isLoading, error } = useEvent(id);

  if (isLoading) return <Loader />;
  if (error) return <Alert color="red">Error: {error.message}</Alert>;
  if (!event) return <div>No event found</div>;

  return <div>{event.name}</div>;
}
```

## React Context Pattern

### When to Use Context vs Zustand
- **Zustand**: Global app state (auth, user, theme)
- **Context**: Component tree state (form wizard, modal state)

### Context Implementation
```typescript
import { createContext, useContext, useState, ReactNode } from 'react';

interface FormWizardContext {
  step: number;
  data: Record<string, any>;
  nextStep: () => void;
  prevStep: () => void;
  updateData: (key: string, value: any) => void;
}

const FormWizardContext = createContext<FormWizardContext | null>(null);

export function FormWizardProvider({ children }: { children: ReactNode }) {
  const [step, setStep] = useState(1);
  const [data, setData] = useState<Record<string, any>>({});

  const nextStep = () => setStep(s => s + 1);
  const prevStep = () => setStep(s => Math.max(1, s - 1));
  const updateData = (key: string, value: any) => {
    setData(d => ({ ...d, [key]: value }));
  };

  return (
    <FormWizardContext.Provider
      value={{ step, data, nextStep, prevStep, updateData }}
    >
      {children}
    </FormWizardContext.Provider>
  );
}

export function useFormWizard() {
  const context = useContext(FormWizardContext);
  if (!context) {
    throw new Error('useFormWizard must be used within FormWizardProvider');
  }
  return context;
}
```

## State Persistence

### LocalStorage Persistence with Zustand
```typescript
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface PreferencesState {
  theme: 'light' | 'dark';
  sidebarCollapsed: boolean;

  setTheme: (theme: 'light' | 'dark') => void;
  toggleSidebar: () => void;
}

export const usePreferencesStore = create<PreferencesState>()(
  persist(
    (set) => ({
      theme: 'light',
      sidebarCollapsed: false,

      setTheme: (theme) => set({ theme }),
      toggleSidebar: () => set((state) => ({
        sidebarCollapsed: !state.sidebarCollapsed,
      })),
    }),
    {
      name: 'user-preferences',  // LocalStorage key
    }
  )
);
```

## Derived State

### Compute Derived Values
```typescript
interface CartState {
  items: Array<{ id: number; price: number; quantity: number }>;

  // Actions
  addItem: (item: { id: number; price: number; quantity: number }) => void;
  removeItem: (id: number) => void;

  // Derived state (computed)
  total: () => number;
  itemCount: () => number;
}

export const useCartStore = create<CartState>((set, get) => ({
  items: [],

  addItem: (item) =>
    set((state) => ({ items: [...state.items, item] })),

  removeItem: (id) =>
    set((state) => ({ items: state.items.filter(i => i.id !== id) })),

  // Derived state computed on demand
  total: () => get().items.reduce((sum, item) => sum + item.price * item.quantity, 0),
  itemCount: () => get().items.reduce((sum, item) => sum + item.quantity, 0),
}));

// Usage
const total = useCartStore(state => state.total());
const itemCount = useCartStore(state => state.itemCount());
```

## State Synchronization

### Sync Multiple Stores
```typescript
// When auth state changes, invalidate user-specific queries
export const useAuthStore = create<AuthState>((set) => ({
  // ... other state ...

  logout: async () => {
    await fetch('/api/auth/logout', { method: 'POST' });
    set({ user: null, isAuthenticated: false });

    // Clear React Query cache on logout
    queryClient.clear();
  },
}));
```

## Optimistic Updates

### Pattern for Immediate UI Updates
```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query';

export function useUpdateEvent(id: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Partial<EventDto>) => {
      const response = await fetch(`/api/events/${id}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      return response.json();
    },
    // Optimistic update
    onMutate: async (newData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['event', id] });

      // Snapshot previous value
      const previousEvent = queryClient.getQueryData(['event', id]);

      // Optimistically update to new value
      queryClient.setQueryData(['event', id], (old: any) => ({
        ...old,
        ...newData,
      }));

      return { previousEvent };
    },
    // Rollback on error
    onError: (err, newData, context) => {
      queryClient.setQueryData(['event', id], context?.previousEvent);
    },
    // Refetch on success or error
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['event', id] });
    },
  });
}
```

## Standards Maintenance

When implementing state management:
1. Use Zustand for global app state
2. Use React Query for API data
3. Use Context for component tree state
4. Avoid object selectors (use individual selectors)
5. Implement optimistic updates for better UX
6. Persist user preferences to LocalStorage

---

*This document is maintained by the React Developer Agent.*
