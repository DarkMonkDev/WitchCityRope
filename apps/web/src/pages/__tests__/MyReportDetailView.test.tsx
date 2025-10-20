import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { MantineProvider } from '@mantine/core';
import { MyReportDetailView } from '../MyReportDetailView';

// Mock useParams to return test ID
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useParams: () => ({ id: '1' })
  };
});

const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <MantineProvider>
      <BrowserRouter>
        <Routes>
          <Route path="*" element={component} />
        </Routes>
      </BrowserRouter>
    </MantineProvider>
  );
};

describe('MyReportDetailView', () => {
  it('renders page title', () => {
    renderWithProviders(<MyReportDetailView />);

    expect(screen.getByText('Your Safety Report')).toBeInTheDocument();
  });

  it('displays back to My Reports link', () => {
    renderWithProviders(<MyReportDetailView />);

    const backLink = screen.getByText('Back to My Reports');
    expect(backLink).toBeInTheDocument();
    expect(backLink.closest('a')).toHaveAttribute('href', '/my-reports');
  });

  it('displays reported and last updated dates', () => {
    renderWithProviders(<MyReportDetailView />);

    expect(screen.getByText('Reported')).toBeInTheDocument();
    const lastUpdatedElements = screen.getAllByText('Last Updated');
    expect(lastUpdatedElements.length).toBeGreaterThan(0);
  });

  it('displays current status alert', () => {
    renderWithProviders(<MyReportDetailView />);

    expect(screen.getByText(/Current Status:/i)).toBeInTheDocument();
  });

  it('displays status explanation', () => {
    renderWithProviders(<MyReportDetailView />);

    // Should show explanation for InformationGathering status
    expect(screen.getByText(/Your report is currently being reviewed/i)).toBeInTheDocument();
  });

  it('renders incident details card', () => {
    renderWithProviders(<MyReportDetailView />);

    // Check for incident description (from mock data)
    expect(screen.getByText(/During a suspension demonstration/i)).toBeInTheDocument();
  });

  it('renders people involved card when data exists', () => {
    renderWithProviders(<MyReportDetailView />);

    // Mock data includes involved parties
    expect(screen.getByText(/RopeExpert123/i)).toBeInTheDocument();
  });

  it('displays contact information card', () => {
    renderWithProviders(<MyReportDetailView />);

    expect(screen.getByText('Need to Provide Additional Information?')).toBeInTheDocument();
    expect(screen.getByText(/safety@witchcityrope.com/i)).toBeInTheDocument();
  });

  it('shows limited view notice', () => {
    renderWithProviders(<MyReportDetailView />);

    expect(screen.getByText(/This is a limited view of your report/i)).toBeInTheDocument();
    expect(screen.getByText(/coordinator information and internal notes are not displayed/i)).toBeInTheDocument();
  });

  it('does not display reference number (user-facing restriction)', () => {
    renderWithProviders(<MyReportDetailView />);

    // Should NOT show SAF- reference number pattern
    expect(screen.queryByText(/SAF-/i)).not.toBeInTheDocument();
  });

  it('does not display coordinator information', () => {
    renderWithProviders(<MyReportDetailView />);

    // Should NOT show coordinator label or name
    expect(screen.queryByText(/Coordinator:/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Safety Coordinator/i)).not.toBeInTheDocument();
  });

  it('does not display notes section', () => {
    renderWithProviders(<MyReportDetailView />);

    // Should NOT show notes section (neither system nor manual)
    expect(screen.queryByText('Notes')).not.toBeInTheDocument();
    expect(screen.queryByPlaceholderText(/Add a note/i)).not.toBeInTheDocument();
  });

  it('does not display Google Drive links', () => {
    renderWithProviders(<MyReportDetailView />);

    // Should NOT show Google Drive section
    expect(screen.queryByText(/Google Drive/i)).not.toBeInTheDocument();
  });

  it('does not display action buttons', () => {
    renderWithProviders(<MyReportDetailView />);

    // Should NOT show admin action buttons
    expect(screen.queryByText(/Assign Coordinator/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Change Status/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Put On Hold/i)).not.toBeInTheDocument();
  });
});
