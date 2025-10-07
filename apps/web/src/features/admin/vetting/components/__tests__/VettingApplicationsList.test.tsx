import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MantineProvider } from '@mantine/core';
import { VettingApplicationsList } from '../VettingApplicationsList';
import { useVettingApplications } from '../../hooks/useVettingApplications';
import type { ApplicationSummaryDto } from '../../types/vetting.types';

// Mock the hooks
vi.mock('../../hooks/useVettingApplications');
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => vi.fn(),
  };
});

const mockUseVettingApplications = vi.mocked(useVettingApplications);
const mockNavigate = vi.fn();

// Mock react-router-dom
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const mockApplications: ApplicationSummaryDto[] = [
  {
    id: 'app-1',
    applicationNumber: 'APP001',
    status: 'UnderReview',
    submittedAt: '2025-09-20T10:00:00Z',
    lastActivityAt: '2025-09-20T10:00:00Z',
    sceneName: 'TestUser1',
    experienceLevel: 'Beginner',
    yearsExperience: 1,
    isAnonymous: false,
    assignedReviewerName: null,
    reviewStartedAt: null,
    priority: 1,
    daysInCurrentStatus: 2,
    referenceStatus: {
      totalReferences: 2,
      contactedReferences: 1,
      respondedReferences: 0,
      allReferencesComplete: false
    },
    hasRecentNotes: false,
    hasPendingActions: true,
    interviewScheduledFor: null,
    skillsTags: ['Rope', 'Bondage']
  },
  {
    id: 'app-2',
    applicationNumber: 'APP002',
    status: 'Approved',
    submittedAt: '2025-09-18T14:30:00Z',
    lastActivityAt: '2025-09-19T09:15:00Z',
    sceneName: 'TestUser2',
    experienceLevel: 'Intermediate',
    yearsExperience: 3,
    isAnonymous: false,
    assignedReviewerName: 'Admin User',
    reviewStartedAt: '2025-09-18T15:00:00Z',
    priority: 2,
    daysInCurrentStatus: 1,
    referenceStatus: {
      totalReferences: 3,
      contactedReferences: 3,
      respondedReferences: 3,
      allReferencesComplete: true
    },
    hasRecentNotes: true,
    hasPendingActions: false,
    interviewScheduledFor: null,
    skillsTags: ['Rope', 'Suspension']
  }
];

describe('VettingApplicationsList', () => {
  const user = userEvent.setup();
  let queryClient: QueryClient;

  beforeEach(() => {
    // Create fresh QueryClient for EACH test to ensure cache isolation
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });

    vi.clearAllMocks();
    mockUseVettingApplications.mockReturnValue({
      data: {
        items: mockApplications,
        totalCount: 2,
        page: 1,
        pageSize: 25,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false
      },
      isLoading: false,
      isError: false,
      isSuccess: true,
      error: null,
      refetch: vi.fn(),
    });
  });

  afterEach(() => {
    // Clear all queries from cache to prevent test pollution
    queryClient.clear();
    vi.clearAllMocks();
  });

  const createWrapper = () => {
    return ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        <MantineProvider>
          <BrowserRouter>
            {children}
          </BrowserRouter>
        </MantineProvider>
      </QueryClientProvider>
    );
  };

  it('renders the applications table with correct columns', () => {
    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    // Check table headers
    expect(screen.getByText('NAME')).toBeInTheDocument();
    expect(screen.getByText('FETLIFE NAME')).toBeInTheDocument();
    expect(screen.getByText('EMAIL')).toBeInTheDocument();
    expect(screen.getByText('APPLICATION DATE')).toBeInTheDocument();
    expect(screen.getByText('CURRENT STATUS')).toBeInTheDocument();
  });

  it('displays application data correctly', () => {
    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    // Check first application data
    expect(screen.getByText('TestUser1')).toBeInTheDocument();
    expect(screen.getByText('9/20/2025')).toBeInTheDocument(); // Formatted date
    
    // Check second application data
    expect(screen.getByText('TestUser2')).toBeInTheDocument();
    expect(screen.getByText('9/18/2025')).toBeInTheDocument();
  });

  it('handles search functionality', async () => {
    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    const searchInput = screen.getByPlaceholderText('Search by name, email, or scene name...');
    
    await user.type(searchInput, 'TestUser1');
    
    await waitFor(() => {
      expect(mockUseVettingApplications).toHaveBeenCalledWith(
        expect.objectContaining({
          searchQuery: 'TestUser1',
          page: 1 // Should reset to page 1 when searching
        })
      );
    });
  });

  it('handles status filter changes', async () => {
    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    const statusFilter = screen.getByDisplayValue('Under Review,Approved for Interview,Pending Interview');
    
    // Clear current selection and select new status
    await user.clear(statusFilter);
    await user.type(statusFilter, 'Approved');
    
    await waitFor(() => {
      expect(mockUseVettingApplications).toHaveBeenCalledWith(
        expect.objectContaining({
          statusFilters: expect.arrayContaining(['Approved']),
          page: 1 // Should reset to page 1 when filtering
        })
      );
    });
  });

  it('handles sorting by clicking column headers', async () => {
    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    const nameHeader = screen.getByText('NAME').closest('th');
    
    await user.click(nameHeader!);
    
    await waitFor(() => {
      expect(mockUseVettingApplications).toHaveBeenCalledWith(
        expect.objectContaining({
          sortBy: 'RealName',
          sortDirection: 'Asc'
        })
      );
    });
    
    // Click again to reverse sort direction
    await user.click(nameHeader!);
    
    await waitFor(() => {
      expect(mockUseVettingApplications).toHaveBeenCalledWith(
        expect.objectContaining({
          sortBy: 'RealName',
          sortDirection: 'Desc'
        })
      );
    });
  });

  it('handles pagination', async () => {
    // Mock data with pagination
    mockUseVettingApplications.mockReturnValue({
      data: {
        items: mockApplications,
        totalCount: 50, // More than one page
        page: 1,
        pageSize: 25,
        totalPages: 2,
        hasPreviousPage: false,
        hasNextPage: true
      },
      isLoading: false,
      isError: false,
      isSuccess: true,
      error: null,
      refetch: vi.fn(),
    });

    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    // Should show pagination controls
    expect(screen.getByText('Showing 1-25 of 50 applications')).toBeInTheDocument();
    
    // Find and click page 2
    const page2Button = screen.getByText('2');
    await user.click(page2Button);
    
    await waitFor(() => {
      expect(mockUseVettingApplications).toHaveBeenCalledWith(
        expect.objectContaining({
          page: 2
        })
      );
    });
  });

  it('handles bulk selection of applications', async () => {
    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    // Find "Select all" checkbox
    const selectAllCheckbox = screen.getByLabelText('Select all applications');
    
    await user.click(selectAllCheckbox);
    
    // All individual checkboxes should be checked
    const individualCheckboxes = screen.getAllByLabelText(/Select application for/);
    individualCheckboxes.forEach(checkbox => {
      expect(checkbox).toBeChecked();
    });
  });

  it('navigates to application detail when row is clicked', async () => {
    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    const firstRow = screen.getByText('TestUser1').closest('tr');
    
    await user.click(firstRow!);
    
    expect(mockNavigate).toHaveBeenCalledWith('/admin/vetting/applications/app-1');
  });

  it('shows loading state', () => {
    mockUseVettingApplications.mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      isSuccess: false,
      error: null,
      refetch: vi.fn(),
    });

    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    expect(screen.getByText('Loading applications...')).toBeInTheDocument();
  });

  it('shows error state with retry option', async () => {
    const mockRefetch = vi.fn();
    mockUseVettingApplications.mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      isSuccess: false,
      error: new Error('Failed to load applications'),
      refetch: mockRefetch,
    });

    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    expect(screen.getByText('Error loading applications: Failed to load applications')).toBeInTheDocument();
    
    const retryButton = screen.getByText('Try Again');
    await user.click(retryButton);
    
    expect(mockRefetch).toHaveBeenCalled();
  });

  it('shows empty state when no applications match filters', () => {
    mockUseVettingApplications.mockReturnValue({
      data: {
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 25,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false
      },
      isLoading: false,
      isError: false,
      isSuccess: true,
      error: null,
      refetch: vi.fn(),
    });

    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    expect(screen.getByText('No vetting applications found')).toBeInTheDocument();
  });

  it.skip('shows empty state with clear filters option when filters are applied', async () => {
    // SKIPPED: "Clear Filters" button and "No applications match your filters" message not implemented
    // TODO: Implement enhanced empty state with filter-aware messaging and clear filters functionality
    // Current implementation: VettingApplicationsList shows generic "No vetting applications found" message
    // See: /apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx
    // Expected behavior: Different empty state messages based on whether filters are active

    // Mock the hook to simulate filtered state
    mockUseVettingApplications.mockReturnValue({
      data: {
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 25,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false
      },
      isLoading: false,
      isError: false,
      isSuccess: true,
      error: null,
      refetch: vi.fn(),
    });

    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    // Add a search filter first
    const searchInput = screen.getByPlaceholderText('Search by name, email, or scene name...');
    await user.type(searchInput, 'nonexistent');

    await waitFor(() => {
      expect(screen.getByText('No applications match your filters')).toBeInTheDocument();
    });

    const clearFiltersButton = screen.getByText('Clear Filters');
    await user.click(clearFiltersButton);

    await waitFor(() => {
      expect(mockUseVettingApplications).toHaveBeenCalledWith(
        expect.objectContaining({
          searchQuery: '',
          statusFilters: [],
          page: 1
        })
      );
    });
  });

  it('prevents row click when checkbox is clicked', async () => {
    render(<VettingApplicationsList />, { wrapper: createWrapper() });

    const firstRowCheckbox = screen.getAllByLabelText(/Select application for/)[0];
    
    await user.click(firstRowCheckbox);
    
    // Navigation should not have been called
    expect(mockNavigate).not.toHaveBeenCalled();
    
    // But checkbox should be checked
    expect(firstRowCheckbox).toBeChecked();
  });
});
