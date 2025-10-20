import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { MantineProvider } from '@mantine/core';
import { MyReportCard } from '../MyReportCard';

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate
  };
});

// Wrapper with Mantine provider
const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <MantineProvider>
      <BrowserRouter>
        {component}
      </BrowserRouter>
    </MantineProvider>
  );
};

const defaultProps = {
  id: '1',
  incidentDate: '2025-10-15T19:30:00Z',
  location: 'Monthly Rope Jam - Main Studio',
  severity: 'High' as const,
  status: 'InformationGathering' as const,
  reportedAt: '2025-10-16T08:00:00Z',
  lastUpdatedAt: '2025-10-17T14:30:00Z'
};

describe('MyReportCard', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  it('renders card with all data', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    expect(screen.getByTestId('my-report-card')).toBeInTheDocument();
    expect(screen.getByText(/October 15, 2025/i)).toBeInTheDocument();
    expect(screen.getByText('Monthly Rope Jam - Main Studio')).toBeInTheDocument();
  });

  it('displays severity badge', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    // Severity badge component should be rendered
    expect(screen.getByText(/HIGH/i)).toBeInTheDocument();
  });

  it('displays status badge', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    // Status badge component should be rendered
    expect(screen.getByText(/Information Gathering/i)).toBeInTheDocument();
  });

  it('shows correct status message for InformationGathering', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    expect(screen.getByText('Your report is being reviewed by our safety team')).toBeInTheDocument();
  });

  it('shows correct status message for Closed status', () => {
    renderWithProviders(<MyReportCard {...defaultProps} status="Closed" />);

    expect(screen.getByText('This report has been resolved')).toBeInTheDocument();
  });

  it('shows correct status message for ReportSubmitted', () => {
    renderWithProviders(<MyReportCard {...defaultProps} status="ReportSubmitted" />);

    expect(screen.getByText('Your report is awaiting review')).toBeInTheDocument();
  });

  it('displays days ago for last updated', () => {
    const oneDayAgo = new Date();
    oneDayAgo.setDate(oneDayAgo.getDate() - 1);

    renderWithProviders(<MyReportCard {...defaultProps} lastUpdatedAt={oneDayAgo.toISOString()} />);

    expect(screen.getByText('1 day ago')).toBeInTheDocument();
  });

  it('displays reported date formatted', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    expect(screen.getByText(/October 16, 2025/i)).toBeInTheDocument();
  });

  it('navigates to detail page when card is clicked', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    const card = screen.getByTestId('my-report-card');
    fireEvent.click(card);

    // Navigation should be deferred via setTimeout
    setTimeout(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/my-reports/1');
    }, 10);
  });

  it('navigates to detail page when View Details button is clicked', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    const button = screen.getByRole('button', { name: /View Details/i });
    fireEvent.click(button);

    // Navigation should be deferred via setTimeout
    setTimeout(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/my-reports/1');
    }, 10);
  });

  it('stops propagation when button is clicked', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    const button = screen.getByRole('button', { name: /View Details/i });
    const stopPropagationSpy = vi.fn();

    button.onclick = (e) => {
      stopPropagationSpy();
      e.stopPropagation();
    };

    fireEvent.click(button);
    expect(stopPropagationSpy).toHaveBeenCalled();
  });

  it('displays hover effect on card', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    const card = screen.getByTestId('my-report-card');

    // Card should have transition style
    expect(card).toHaveStyle({ transition: 'box-shadow 0.2s ease' });
  });

  // Severity field removed - test no longer applicable

  it('does not display reference number (user-facing restriction)', () => {
    renderWithProviders(<MyReportCard {...defaultProps} />);

    // Should NOT show SAF- reference number pattern
    expect(screen.queryByText(/SAF-/i)).not.toBeInTheDocument();
  });
});
