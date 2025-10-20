import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PeopleInvolvedCard } from '../PeopleInvolvedCard';

describe('PeopleInvolvedCard', () => {
  it('renders involved parties when provided', () => {
    render(
      <PeopleInvolvedCard
        involvedParties="Top: RopeExpert\nBottom: Participant"
        witnesses={undefined}
      />
    );
    expect(screen.getByText('Involved Parties')).toBeInTheDocument();
    expect(screen.getByText(/Top: RopeExpert/)).toBeInTheDocument();
  });

  it('renders witnesses when provided', () => {
    render(
      <PeopleInvolvedCard
        involvedParties={undefined}
        witnesses="Witness 1\nWitness 2"
      />
    );
    expect(screen.getByText('Witnesses')).toBeInTheDocument();
    expect(screen.getByText(/Witness 1/)).toBeInTheDocument();
  });

  it('renders both involved parties and witnesses', () => {
    render(
      <PeopleInvolvedCard
        involvedParties="Top: RopeExpert\nBottom: Participant"
        witnesses="Witness 1\nWitness 2"
      />
    );
    expect(screen.getByText('Involved Parties')).toBeInTheDocument();
    expect(screen.getByText('Witnesses')).toBeInTheDocument();
  });

  it('shows empty state when no people documented', () => {
    render(<PeopleInvolvedCard involvedParties={undefined} witnesses={undefined} />);
    expect(screen.getByText('No people documented')).toBeInTheDocument();
  });

  it('does not show empty state when only involved parties provided', () => {
    render(
      <PeopleInvolvedCard
        involvedParties="Top: RopeExpert"
        witnesses={undefined}
      />
    );
    expect(screen.queryByText('No people documented')).not.toBeInTheDocument();
  });

  it('does not show empty state when only witnesses provided', () => {
    render(
      <PeopleInvolvedCard
        involvedParties={undefined}
        witnesses="Witness 1"
      />
    );
    expect(screen.queryByText('No people documented')).not.toBeInTheDocument();
  });

  it('preserves whitespace in involved parties text', () => {
    render(
      <PeopleInvolvedCard
        involvedParties="Line 1\nLine 2"
        witnesses={undefined}
      />
    );
    const textElement = screen.getByText(/Line 1/);
    expect(textElement).toHaveStyle({ whiteSpace: 'pre-wrap' });
  });

  it('preserves whitespace in witnesses text', () => {
    render(
      <PeopleInvolvedCard
        involvedParties={undefined}
        witnesses="Witness 1\nWitness 2"
      />
    );
    const textElement = screen.getByText(/Witness 1/);
    expect(textElement).toHaveStyle({ whiteSpace: 'pre-wrap' });
  });
});
