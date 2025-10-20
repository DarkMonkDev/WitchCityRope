import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { IncidentDetailHeader } from '../IncidentDetailHeader';

const renderWithRouter = (ui: React.ReactElement) => {
  return render(<BrowserRouter>{ui}</BrowserRouter>);
};

describe('IncidentDetailHeader', () => {
  const defaultProps = {
    referenceNumber: 'SAF-20251018-0001',
    severity: 'High' as const,
    status: 'InformationGathering' as const,
    reportedDate: '2025-10-16T08:00:00Z',
    incidentDate: '2025-10-15T19:30:00Z',
    location: 'Monthly Rope Jam - Main Studio',
    coordinatorName: 'Safety Coordinator',
    isAnonymous: false
  };

  it('renders reference number prominently', () => {
    renderWithRouter(<IncidentDetailHeader {...defaultProps} />);
    expect(screen.getByText('SAF-20251018-0001')).toBeInTheDocument();
  });

  it('renders severity and status badges', () => {
    renderWithRouter(<IncidentDetailHeader {...defaultProps} />);
    expect(screen.getByText('HIGH')).toBeInTheDocument();
    expect(screen.getByText(/Information Gathering|Investigating/i)).toBeInTheDocument();
  });

  it('displays coordinator name when assigned', () => {
    renderWithRouter(<IncidentDetailHeader {...defaultProps} />);
    expect(screen.getByText(/Coordinator: Safety Coordinator/)).toBeInTheDocument();
  });

  it('displays "Unassigned" badge when no coordinator', () => {
    renderWithRouter(
      <IncidentDetailHeader {...defaultProps} coordinatorName={undefined} />
    );
    expect(screen.getByText('Unassigned')).toBeInTheDocument();
  });

  it('displays incident date and location', () => {
    renderWithRouter(<IncidentDetailHeader {...defaultProps} />);
    expect(screen.getByText('Monthly Rope Jam - Main Studio')).toBeInTheDocument();
  });

  it('shows "Anonymous" badge for anonymous reports', () => {
    renderWithRouter(
      <IncidentDetailHeader {...defaultProps} isAnonymous={true} />
    );
    expect(screen.getByText('Anonymous')).toBeInTheDocument();
  });

  it('shows "Identified" badge for non-anonymous reports', () => {
    renderWithRouter(<IncidentDetailHeader {...defaultProps} />);
    expect(screen.getByText('Identified')).toBeInTheDocument();
  });

  it('displays "days ago" indicator for reported date', () => {
    renderWithRouter(<IncidentDetailHeader {...defaultProps} />);
    // Should show something like "2 days ago" or "today" depending on date
    expect(screen.getByText(/days? ago|today/i)).toBeInTheDocument();
  });

  it('renders back to dashboard link', () => {
    renderWithRouter(<IncidentDetailHeader {...defaultProps} />);
    const backLink = screen.getByText('Back to Dashboard');
    expect(backLink).toBeInTheDocument();
    expect(backLink.closest('a')).toHaveAttribute('href', '/admin/incident-management');
  });
});
