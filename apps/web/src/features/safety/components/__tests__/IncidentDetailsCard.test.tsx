import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MantineProvider } from '@mantine/core';
import { IncidentDetailsCard } from '../IncidentDetailsCard';

const renderWithProvider = (ui: React.ReactElement) => {
  return render(
    <MantineProvider>
      {ui}
    </MantineProvider>
  );
};

describe('IncidentDetailsCard', () => {
  const defaultProps = {
    description: 'This is a detailed description of the incident.',
    isAnonymous: false,
    reporterName: 'Jane Rigger',
    reporterEmail: 'jane@example.com',
    requestedFollowUp: true,
    createdAt: '2025-10-16T08:00:00Z',
    updatedAt: '2025-10-17T14:30:00Z'
  };

  it('renders incident description', () => {
    renderWithProvider(<IncidentDetailsCard {...defaultProps} />);
    expect(screen.getByText('This is a detailed description of the incident.')).toBeInTheDocument();
  });

  it('displays reporter name and email for identified reports', () => {
    renderWithProvider(<IncidentDetailsCard {...defaultProps} />);
    expect(screen.getByText('Jane Rigger')).toBeInTheDocument();
    expect(screen.getByText('jane@example.com')).toBeInTheDocument();
  });

  it('shows follow-up requested badge when requested', () => {
    renderWithProvider(<IncidentDetailsCard {...defaultProps} />);
    expect(screen.getByText('Follow-up Requested')).toBeInTheDocument();
  });

  it('does not show follow-up badge when not requested', () => {
    renderWithProvider(<IncidentDetailsCard {...defaultProps} requestedFollowUp={false} />);
    expect(screen.queryByText('Follow-up Requested')).not.toBeInTheDocument();
  });

  it('displays "Anonymous Report" for anonymous submissions', () => {
    renderWithProvider(
      <IncidentDetailsCard
        {...defaultProps}
        isAnonymous={true}
        reporterName={undefined}
        reporterEmail={undefined}
      />
    );
    expect(screen.getByText('Anonymous Report')).toBeInTheDocument();
    expect(screen.getByText('No follow-up capability')).toBeInTheDocument();
  });

  it('does not display reporter info for anonymous reports', () => {
    renderWithProvider(
      <IncidentDetailsCard
        {...defaultProps}
        isAnonymous={true}
        reporterName={undefined}
        reporterEmail={undefined}
      />
    );
    expect(screen.queryByText('jane@example.com')).not.toBeInTheDocument();
  });

  it('displays created and updated timestamps', () => {
    renderWithProvider(<IncidentDetailsCard {...defaultProps} />);
    expect(screen.getByText('Created')).toBeInTheDocument();
    expect(screen.getByText('Last Updated')).toBeInTheDocument();
  });

  it('preserves whitespace in description', () => {
    const multiLineDescription = 'Line 1\n\nLine 2\nLine 3';
    renderWithProvider(<IncidentDetailsCard {...defaultProps} description={multiLineDescription} />);
    const descriptionElement = screen.getByText((content, element) => {
      return element?.textContent === multiLineDescription;
    });
    expect(descriptionElement).toHaveStyle({ whiteSpace: 'pre-wrap' });
  });
});
