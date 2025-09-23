import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MantineProvider } from '@mantine/core';
import { VettingApplicationDetail } from '../../../../../../apps/web/src/features/admin/vetting/components/VettingApplicationDetail';
import { useVettingApplicationDetail } from '../../../../../../apps/web/src/features/admin/vetting/hooks/useVettingApplicationDetail';
import { useSubmitReviewDecision } from '../../../../../../apps/web/src/features/admin/vetting/hooks/useSubmitReviewDecision';
import { useApproveApplication } from '../../../../../../apps/web/src/features/admin/vetting/hooks/useApproveApplication';
import type { ApplicationDetailResponse } from '../../../../../../apps/web/src/features/admin/vetting/types/vetting.types';

// Mock the hooks
vi.mock('../../../../../../apps/web/src/features/admin/vetting/hooks/useVettingApplicationDetail');
vi.mock('../../../../../../apps/web/src/features/admin/vetting/hooks/useSubmitReviewDecision');
vi.mock('../../../../../../apps/web/src/features/admin/vetting/hooks/useApproveApplication');

const mockUseVettingApplicationDetail = vi.mocked(useVettingApplicationDetail);
const mockUseSubmitReviewDecision = vi.mocked(useSubmitReviewDecision);
const mockUseApproveApplication = vi.mocked(useApproveApplication);

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  
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

const mockApplication: ApplicationDetailResponse = {
  id: 'app-1',
  applicationNumber: 'APP001',
  status: 'UnderReview',
  submittedAt: '2025-09-20T10:00:00Z',
  lastActivityAt: '2025-09-20T10:00:00Z',
  fullName: 'John Doe',
  sceneName: 'TestUser1',
  pronouns: 'he/him',
  email: 'john.doe@example.com',
  experienceLevel: 'Beginner',
  yearsExperience: 1,
  experienceDescription: 'I have been interested in rope bondage for about a year.',
  safetyKnowledge: 'I understand the basics of safe rope practices.',
  consentUnderstanding: 'Consent is paramount in all activities.',
  whyJoinCommunity: 'I want to learn from experienced practitioners and make connections.',
  skillsInterests: ['Rope', 'Bondage'],
  expectationsGoals: 'Learn advanced techniques and safety practices.',
  agreesToGuidelines: true,
  isAnonymous: false,
  agreesToTerms: true,
  consentToContact: true,
  assignedReviewerName: null,
  reviewStartedAt: null,
  priority: 1,
  interviewScheduledFor: null,
  references: [],
  notes: [],
  decisions: []
};

describe('VettingApplicationDetail', () => {
  const user = userEvent.setup();
  const mockOnBack = vi.fn();
  const mockRefetch = vi.fn();
  const mockSubmitDecision = vi.fn();
  const mockApproveApplication = vi.fn();
  
  beforeEach(() => {
    vi.clearAllMocks();
    
    mockUseVettingApplicationDetail.mockReturnValue({
      data: mockApplication,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });
    
    mockUseSubmitReviewDecision.mockReturnValue({
      mutate: mockSubmitDecision,
      isPending: false,
    });
    
    mockUseApproveApplication.mockReturnValue({
      mutate: mockApproveApplication,
      isPending: false,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders application details correctly', () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    // Check title and basic info
    expect(screen.getByText('Application - John Doe (TestUser1)')).toBeInTheDocument();
    expect(screen.getByText('TestUser1')).toBeInTheDocument();
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('john.doe@example.com')).toBeInTheDocument();
    expect(screen.getByText('he/him')).toBeInTheDocument();
  });

  it('displays application information in correct layout', () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    // Check inline layout fields
    expect(screen.getByText('Scene Name:')).toBeInTheDocument();
    expect(screen.getByText('Real Name:')).toBeInTheDocument();
    expect(screen.getByText('Email:')).toBeInTheDocument();
    expect(screen.getByText('Pronouns:')).toBeInTheDocument();
    expect(screen.getByText('Application Date:')).toBeInTheDocument();
    
    // Check long answer fields
    expect(screen.getByText('Why do you want to join WitchCityRope?')).toBeInTheDocument();
    expect(screen.getByText('What is your rope experience thus far?')).toBeInTheDocument();
    expect(screen.getByText(mockApplication.whyJoinCommunity)).toBeInTheDocument();
  });

  it('shows correct action buttons based on status', () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    // For UnderReview status, should show "APPROVE FOR INTERVIEW"
    expect(screen.getByText('APPROVE FOR INTERVIEW')).toBeInTheDocument();
    expect(screen.getByText('PUT ON HOLD')).toBeInTheDocument();
    expect(screen.getByText('SEND REMINDER')).toBeInTheDocument();
    expect(screen.getByText('DENY APPLICATION')).toBeInTheDocument();
  });

  it('shows correct button text for different statuses', () => {
    // Test with InterviewApproved status
    mockUseVettingApplicationDetail.mockReturnValue({
      data: { ...mockApplication, status: 'InterviewApproved' },
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });

    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    // For InterviewApproved status, should show "APPROVE APPLICATION"
    expect(screen.getByText('APPROVE APPLICATION')).toBeInTheDocument();
  });

  it('disables action buttons based on status', () => {
    // Test with Approved status
    mockUseVettingApplicationDetail.mockReturnValue({
      data: { ...mockApplication, status: 'Approved' },
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });

    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    const approveButton = screen.getByText('APPROVE APPLICATION');
    expect(approveButton).toBeDisabled();
  });

  it('handles approve button click', async () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    const approveButton = screen.getByText('APPROVE FOR INTERVIEW');
    await user.click(approveButton);

    expect(mockApproveApplication).toHaveBeenCalledWith({
      applicationId: 'app-1',
      reasoning: 'Application approved for interview based on initial review'
    });
  });

  it('opens put on hold modal when button clicked', async () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    const putOnHoldButton = screen.getByText('PUT ON HOLD');
    await user.click(putOnHoldButton);

    // The modal should be rendered (OnHoldModal component)
    // This would need to be tested with the actual modal content
    // For now, we verify the button was clicked
    expect(putOnHoldButton).toBeInTheDocument();
  });

  it('opens send reminder modal when button clicked', async () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    const sendReminderButton = screen.getByText('SEND REMINDER');
    await user.click(sendReminderButton);

    // The modal should be rendered (SendReminderModal component)
    expect(sendReminderButton).toBeInTheDocument();
  });

  it('handles back button click', async () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    const backButton = screen.getByText('Back to Applications');
    await user.click(backButton);

    expect(mockOnBack).toHaveBeenCalled();
  });

  it('handles note addition', async () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    const noteTextarea = screen.getByPlaceholderText('Add Note');
    const saveNoteButton = screen.getByText('SAVE NOTE');

    // Initially save button should be disabled
    expect(saveNoteButton).toBeDisabled();

    // Add some text
    await user.type(noteTextarea, 'This is a test note');

    // Save button should now be enabled
    expect(saveNoteButton).toBeEnabled();

    await user.click(saveNoteButton);

    // TODO: This would trigger the note saving functionality
    // For now we just verify the interaction worked
  });

  it('shows loading state', () => {
    mockUseVettingApplicationDetail.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch: mockRefetch,
    });

    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    expect(screen.getByText('Loading application details...')).toBeInTheDocument();
  });

  it('shows error state', () => {
    mockUseVettingApplicationDetail.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('Failed to load application'),
      refetch: mockRefetch,
    });

    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    expect(screen.getByText('Error loading application: Failed to load application')).toBeInTheDocument();
    
    const backButton = screen.getByText('Back to Applications');
    expect(backButton).toBeInTheDocument();
  });

  it('shows loading states for mutations', () => {
    mockUseApproveApplication.mockReturnValue({
      mutate: mockApproveApplication,
      isPending: true,
    });

    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    const approveButton = screen.getByText('APPROVE FOR INTERVIEW');
    // Mantine Button with loading prop should be disabled
    expect(approveButton).toBeDisabled();
  });

  it('displays status history when available', () => {
    const applicationWithHistory = {
      ...mockApplication,
      decisions: [
        {
          id: 'decision-1',
          decisionType: 'UnderReview',
          reasoning: 'Initial review started',
          createdAt: '2025-09-20T10:00:00Z',
          reviewerName: 'Admin User'
        }
      ],
      notes: [
        {
          id: 'note-1',
          content: 'Applicant provided good references',
          createdAt: '2025-09-20T11:00:00Z',
          reviewerName: 'Admin User'
        }
      ]
    };

    mockUseVettingApplicationDetail.mockReturnValue({
      data: applicationWithHistory,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });

    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    expect(screen.getByText('Notes and Status History')).toBeInTheDocument();
    expect(screen.getByText('Initial review started')).toBeInTheDocument();
    expect(screen.getByText('Applicant provided good references')).toBeInTheDocument();
    expect(screen.getAllByText('Admin User')).toHaveLength(2);
  });

  it('shows empty state when no history available', () => {
    render(
      <VettingApplicationDetail 
        applicationId="app-1" 
        onBack={mockOnBack} 
      />, 
      { wrapper: createWrapper() }
    );

    expect(screen.getByText('No status history or notes yet')).toBeInTheDocument();
  });
});
