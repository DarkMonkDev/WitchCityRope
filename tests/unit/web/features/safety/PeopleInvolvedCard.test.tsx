import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MantineProvider } from '@mantine/core';
import { PeopleInvolvedCard } from '@/features/safety/components/PeopleInvolvedCard';

const renderWithProvider = (ui: React.ReactElement) => {
  return render(
    <MantineProvider>
      {ui}
    </MantineProvider>
  );
};

describe('PeopleInvolvedCard', () => {
  it('renders involved parties when provided', () => {
    renderWithProvider(
      <PeopleInvolvedCard
        involvedParties="Top: RopeExpert\nBottom: Participant"
        witnesses={undefined}
        onEditCoordinator={() => {}}
        onEditInvolvedParties={() => {}}
        onEditWitnesses={() => {}}
      />
    );
    expect(screen.getByText('Involved Parties')).toBeInTheDocument();
    expect(screen.getByText(/Top: RopeExpert/)).toBeInTheDocument();
  });

  it('renders witnesses when provided', () => {
    renderWithProvider(
      <PeopleInvolvedCard
        involvedParties={undefined}
        witnesses="Witness 1\nWitness 2"
        onEditCoordinator={() => {}}
        onEditInvolvedParties={() => {}}
        onEditWitnesses={() => {}}
      />
    );
    expect(screen.getByText('Witnesses')).toBeInTheDocument();
    expect(screen.getByText(/Witness 1/)).toBeInTheDocument();
  });

  it('renders both involved parties and witnesses', () => {
    renderWithProvider(
      <PeopleInvolvedCard
        involvedParties="Top: RopeExpert\nBottom: Participant"
        witnesses="Witness 1\nWitness 2"
        onEditCoordinator={() => {}}
        onEditInvolvedParties={() => {}}
        onEditWitnesses={() => {}}
      />
    );
    expect(screen.getByText('Involved Parties')).toBeInTheDocument();
    expect(screen.getByText('Witnesses')).toBeInTheDocument();
  });

  it('shows empty state when no people documented', () => {
    renderWithProvider(
      <PeopleInvolvedCard
        involvedParties={undefined}
        witnesses={undefined}
        onEditCoordinator={() => {}}
        onEditInvolvedParties={() => {}}
        onEditWitnesses={() => {}}
      />
    );
    // Component now shows "None" for each field when empty
    const noneTexts = screen.getAllByText('None');
    expect(noneTexts.length).toBeGreaterThanOrEqual(2); // At least involved parties and witnesses show "None"
  });

  it('does not show empty state when only involved parties provided', () => {
    renderWithProvider(
      <PeopleInvolvedCard
        involvedParties="Top: RopeExpert"
        witnesses={undefined}
        onEditCoordinator={() => {}}
        onEditInvolvedParties={() => {}}
        onEditWitnesses={() => {}}
      />
    );
    // Witnesses should show "None" but involved parties should show actual content
    expect(screen.getByText(/Top: RopeExpert/)).toBeInTheDocument();
  });

  it('does not show empty state when only witnesses provided', () => {
    renderWithProvider(
      <PeopleInvolvedCard
        involvedParties={undefined}
        witnesses="Witness 1"
        onEditCoordinator={() => {}}
        onEditInvolvedParties={() => {}}
        onEditWitnesses={() => {}}
      />
    );
    // Involved parties should show "None" but witnesses should show actual content
    expect(screen.getByText(/Witness 1/)).toBeInTheDocument();
  });

  it('preserves whitespace in involved parties text', () => {
    renderWithProvider(
      <PeopleInvolvedCard
        involvedParties="Line 1\nLine 2"
        witnesses={undefined}
        onEditCoordinator={() => {}}
        onEditInvolvedParties={() => {}}
        onEditWitnesses={() => {}}
      />
    );
    const textElement = screen.getByText(/Line 1/);
    expect(textElement).toHaveStyle({ whiteSpace: 'pre-wrap' });
  });

  it('preserves whitespace in witnesses text', () => {
    renderWithProvider(
      <PeopleInvolvedCard
        involvedParties={undefined}
        witnesses="Witness 1\nWitness 2"
        onEditCoordinator={() => {}}
        onEditInvolvedParties={() => {}}
        onEditWitnesses={() => {}}
      />
    );
    const textElement = screen.getByText(/Witness 1/);
    expect(textElement).toHaveStyle({ whiteSpace: 'pre-wrap' });
  });
});
