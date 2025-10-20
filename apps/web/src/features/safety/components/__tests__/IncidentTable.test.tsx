import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { IncidentTable } from '../IncidentTable';
import type { SafetyIncidentDto } from '../../types/safety.types';
import { IncidentStatus } from '../../types/safety.types';

// Wrapper with Mantine provider
const renderWithMantine = (ui: React.ReactElement) => {
  return render(
    <MantineProvider>
      {ui}
    </MantineProvider>
  );
};

describe('IncidentTable', () => {
  const mockIncidents: SafetyIncidentDto[] = [
    {
      id: '1',
      referenceNumber: 'SAF-20251018-0001',
      title: 'Test Incident',
      status: IncidentStatus.ReportSubmitted,
      incidentDate: '2025-10-15T19:30:00Z',
      reportedAt: '2025-10-15T20:45:00Z',
      location: 'Test Location',
      description: 'Test description',
      isAnonymous: false,
      requestFollowUp: true,
      coordinatorId: null,
      coordinatorName: null,
      auditTrail: [],
      createdAt: '2025-10-15T20:45:00Z',
      updatedAt: '2025-10-18T10:00:00Z'
    },
    {
      id: '2',
      referenceNumber: 'SAF-20251017-0001',
      title: 'Workshop Safety Concern',
      status: IncidentStatus.InformationGathering,
      incidentDate: '2025-10-16T18:00:00Z',
      reportedAt: '2025-10-16T19:30:00Z',
      location: 'Workshop Space',
      description: 'Another incident',
      isAnonymous: true,
      requestFollowUp: false,
      coordinatorId: 'user-123',
      coordinatorName: 'JaneRigger',
      auditTrail: [],
      createdAt: '2025-10-16T19:30:00Z',
      updatedAt: '2025-10-17T14:22:00Z'
    }
  ];

  const mockOnRowClick = vi.fn();
  const mockOnAssign = vi.fn();
  const mockOnPutOnHold = vi.fn();
  const mockOnClose = vi.fn();
  const mockOnClearFilters = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders table headers correctly', () => {
    renderWithMantine(
      <IncidentTable
        incidents={mockIncidents}
        onRowClick={mockOnRowClick}
      />
    );

    expect(screen.getByText(/incident/i)).toBeInTheDocument();
    expect(screen.getByText(/status/i)).toBeInTheDocument();
    expect(screen.getByText(/coordinator/i)).toBeInTheDocument();
    expect(screen.getByText(/last updated/i)).toBeInTheDocument();
    expect(screen.getByText(/actions/i)).toBeInTheDocument();
  });

  it('renders incident data correctly', () => {
    renderWithMantine(
      <IncidentTable
        incidents={mockIncidents}
        onRowClick={mockOnRowClick}
      />
    );

    expect(screen.getByText('SAF-20251018-0001')).toBeInTheDocument();
    expect(screen.getByText('SAF-20251017-0001')).toBeInTheDocument();
    expect(screen.getByText('JaneRigger')).toBeInTheDocument();
    expect(screen.getByText('Unassigned')).toBeInTheDocument();
  });

  it('shows loading state when isLoading is true', () => {
    renderWithMantine(
      <IncidentTable
        incidents={[]}
        isLoading={true}
        onRowClick={mockOnRowClick}
      />
    );

    // Skeleton loaders should be visible
    const skeletons = document.querySelectorAll('[class*="Skeleton"]');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('shows empty state when no incidents', () => {
    renderWithMantine(
      <IncidentTable
        incidents={[]}
        onRowClick={mockOnRowClick}
        onClearFilters={mockOnClearFilters}
      />
    );

    expect(screen.getByText(/no incidents match your filters/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /clear filters/i })).toBeInTheDocument();
  });

  it('calls onRowClick when row is clicked', () => {
    renderWithMantine(
      <IncidentTable
        incidents={mockIncidents}
        onRowClick={mockOnRowClick}
      />
    );

    const firstRow = screen.getByText('SAF-20251018-0001').closest('tr');
    expect(firstRow).toBeInTheDocument();

    if (firstRow) {
      fireEvent.click(firstRow);
      expect(mockOnRowClick).toHaveBeenCalledWith('1');
    }
  });

  it('renders incident titles', () => {
    renderWithMantine(
      <IncidentTable
        incidents={mockIncidents}
        onRowClick={mockOnRowClick}
      />
    );

    // Incident titles should render
    expect(screen.getByText('Test Incident')).toBeInTheDocument();
    expect(screen.getByText('Workshop Safety Concern')).toBeInTheDocument();
  });

  it('renders status badges', () => {
    renderWithMantine(
      <IncidentTable
        incidents={mockIncidents}
        onRowClick={mockOnRowClick}
      />
    );

    // IncidentStatusBadge components should render (short labels on sm size)
    expect(screen.getByText(/submitted/i)).toBeInTheDocument();
    expect(screen.getByText(/investigating/i)).toBeInTheDocument();
  });

  it('shows action menu with all options', () => {
    renderWithMantine(
      <IncidentTable
        incidents={mockIncidents}
        onRowClick={mockOnRowClick}
        onAssign={mockOnAssign}
        onPutOnHold={mockOnPutOnHold}
        onClose={mockOnClose}
      />
    );

    // Find first action button (dots menu)
    const actionButtons = screen.getAllByRole('button', { hidden: true });
    const firstActionButton = actionButtons.find(btn =>
      btn.getAttribute('class')?.includes('ActionIcon')
    );

    expect(firstActionButton).toBeInTheDocument();
  });

  it('displays "Unassigned" for incidents without coordinator', () => {
    renderWithMantine(
      <IncidentTable
        incidents={mockIncidents}
        onRowClick={mockOnRowClick}
      />
    );

    const unassignedText = screen.getByText('Unassigned');
    expect(unassignedText).toBeInTheDocument();
    expect(unassignedText).toHaveStyle({ fontWeight: '600' });
  });

  it('displays coordinator name for assigned incidents', () => {
    renderWithMantine(
      <IncidentTable
        incidents={mockIncidents}
        onRowClick={mockOnRowClick}
      />
    );

    expect(screen.getByText('JaneRigger')).toBeInTheDocument();
  });

  it('formats relative time correctly', () => {
    // Create incident updated today
    const todayIncident: SafetyIncidentDto = {
      ...mockIncidents[0],
      id: '3',
      updatedAt: new Date().toISOString()
    };

    renderWithMantine(
      <IncidentTable
        incidents={[todayIncident]}
        onRowClick={mockOnRowClick}
      />
    );

    expect(screen.getByText(/today/i)).toBeInTheDocument();
  });

  it('applies aging indicators to old incidents', () => {
    // Create old incident (>7 days)
    const oldDate = new Date();
    oldDate.setDate(oldDate.getDate() - 10);

    const oldIncident: SafetyIncidentDto = {
      ...mockIncidents[0],
      id: '4',
      updatedAt: oldDate.toISOString()
    };

    renderWithMantine(
      <IncidentTable
        incidents={[oldIncident]}
        onRowClick={mockOnRowClick}
      />
    );

    // Should show relative time with red color for >7 days
    const timeText = screen.getByText(/days ago|week ago/i);
    expect(timeText).toBeInTheDocument();
    expect(timeText).toHaveStyle({ color: '#AA0130' }); // Red color
  });
});
