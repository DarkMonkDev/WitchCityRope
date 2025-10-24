import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { IncidentStatusBadge } from '../IncidentStatusBadge';
import { MantineProvider } from '@mantine/core';

const renderWithMantine = (component: React.ReactElement) => {
  return render(
    <MantineProvider>
      {component}
    </MantineProvider>
  );
};

describe('IncidentStatusBadge', () => {
  it('renders ReportSubmitted status with correct label', () => {
    renderWithMantine(<IncidentStatusBadge status="ReportSubmitted" />);

    expect(screen.getByText('Submitted')).toBeInTheDocument();
  });

  it('renders InformationGathering status with correct label', () => {
    renderWithMantine(<IncidentStatusBadge status="InformationGathering" />);

    expect(screen.getByText('Investigating')).toBeInTheDocument();
  });

  it('renders ReviewingFinalReport status with correct label', () => {
    renderWithMantine(<IncidentStatusBadge status="ReviewingFinalReport" />);

    expect(screen.getByText('Final Review')).toBeInTheDocument();
  });

  it('renders OnHold status with correct label', () => {
    renderWithMantine(<IncidentStatusBadge status="OnHold" />);

    expect(screen.getByText('On Hold')).toBeInTheDocument();
  });

  it('renders Closed status with correct label', () => {
    renderWithMantine(<IncidentStatusBadge status="Closed" />);

    expect(screen.getByText('Closed')).toBeInTheDocument();
  });

  it('shows full label on large size', () => {
    renderWithMantine(<IncidentStatusBadge status="InformationGathering" size="lg" />);

    expect(screen.getByText('Information Gathering')).toBeInTheDocument();
  });

  it('shows short label on small size', () => {
    renderWithMantine(<IncidentStatusBadge status="InformationGathering" size="sm" />);

    expect(screen.getByText('Investigating')).toBeInTheDocument();
  });

  it('applies correct ARIA label for ReportSubmitted', () => {
    renderWithMantine(
      <IncidentStatusBadge status="ReportSubmitted" data-testid="status-badge" />
    );

    const badge = screen.getByTestId('status-badge');
    expect(badge).toHaveAttribute('aria-label', 'Report submitted, awaiting assignment');
  });

  it('applies correct ARIA label for InformationGathering', () => {
    renderWithMantine(
      <IncidentStatusBadge status="InformationGathering" data-testid="status-badge" />
    );

    const badge = screen.getByTestId('status-badge');
    expect(badge).toHaveAttribute('aria-label', 'Information gathering in progress');
  });

  it('applies correct ARIA label for Closed', () => {
    renderWithMantine(<IncidentStatusBadge status="Closed" data-testid="status-badge" />);

    const badge = screen.getByTestId('status-badge');
    expect(badge).toHaveAttribute('aria-label', 'Incident closed and archived');
  });

  it('applies correct CSS class for status', () => {
    renderWithMantine(<IncidentStatusBadge status="OnHold" data-testid="status-badge" />);

    const badge = screen.getByTestId('status-badge');
    expect(badge).toHaveClass('status-onhold');
  });
});
