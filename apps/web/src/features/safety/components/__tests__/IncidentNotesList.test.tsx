import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MantineProvider } from '@mantine/core';
import { IncidentNotesList, IncidentNoteDto, IncidentNoteType } from '../IncidentNotesList';

describe('IncidentNotesList', () => {
  const mockSystemNote: IncidentNoteDto = {
    id: 'n1',
    incidentId: '1',
    authorId: 'system',
    authorName: 'System',
    content: 'Status changed from Report Submitted to Information Gathering',
    noteType: IncidentNoteType.System,
    isPrivate: true,
    createdAt: '2025-10-16T09:00:00Z'
  };

  const mockManualNote: IncidentNoteDto = {
    id: 'n2',
    incidentId: '1',
    authorId: 'user1',
    authorName: 'Safety Coordinator',
    content: 'Initial contact made with reporter.',
    noteType: IncidentNoteType.Manual,
    isPrivate: false,
    tags: ['initial-contact'],
    createdAt: '2025-10-17T10:00:00Z'
  };

  const mockOnAddNote = vi.fn().mockResolvedValue(undefined);

  const renderWithProvider = (ui: React.ReactElement) => {
    return render(
      <MantineProvider>
        {ui}
      </MantineProvider>
    );
  };

  it('renders add note form', () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} />);
    expect(screen.getByPlaceholderText('Add a note about this incident...')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Save Note/i })).toBeInTheDocument();
  });

  it('disables save button when note is empty', () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} />);
    const saveButton = screen.getByRole('button', { name: /Save Note/i });
    expect(saveButton).toBeDisabled();
  });

  it('enables save button when note has content', async () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} />);
    const textarea = screen.getByPlaceholderText('Add a note about this incident...');
    const saveButton = screen.getByRole('button', { name: /Save Note/i });

    fireEvent.change(textarea, { target: { value: 'Test note content' } });

    await waitFor(() => {
      expect(saveButton).not.toBeDisabled();
    });
  });

  it('renders system notes with correct styling', () => {
    renderWithProvider(<IncidentNotesList notes={[mockSystemNote]} onAddNote={mockOnAddNote} />);

    expect(screen.getByText('SYSTEM')).toBeInTheDocument();
    expect(screen.getByText('System')).toBeInTheDocument();
    expect(screen.getByText('Status changed from Report Submitted to Information Gathering')).toBeInTheDocument();

    const systemNoteElement = screen.getByTestId('system-note');
    expect(systemNoteElement).toBeInTheDocument();
  });

  it('renders manual notes with privacy indicator', () => {
    renderWithProvider(<IncidentNotesList notes={[mockManualNote]} onAddNote={mockOnAddNote} />);

    expect(screen.getByText('Safety Coordinator')).toBeInTheDocument();
    expect(screen.getByText('Initial contact made with reporter.')).toBeInTheDocument();
    expect(screen.getByText('Shared')).toBeInTheDocument(); // Because isPrivate = false

    const manualNoteElement = screen.getByTestId('manual-note');
    expect(manualNoteElement).toBeInTheDocument();
  });

  it('displays "Private" badge for private manual notes', () => {
    const privateNote: IncidentNoteDto = {
      ...mockManualNote,
      isPrivate: true
    };

    renderWithProvider(<IncidentNotesList notes={[privateNote]} onAddNote={mockOnAddNote} />);
    expect(screen.getByText('Private')).toBeInTheDocument();
  });

  it('displays tags when present', () => {
    renderWithProvider(<IncidentNotesList notes={[mockManualNote]} onAddNote={mockOnAddNote} />);
    expect(screen.getByText('initial-contact')).toBeInTheDocument();
  });

  it('shows empty state when no notes', () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} />);
    expect(screen.getByText('No notes added yet')).toBeInTheDocument();
  });

  it('renders privacy toggle switch', () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} />);
    expect(screen.getByText(/Private \(coordinators only\)/i)).toBeInTheDocument();
  });

  it('changes privacy label when toggled', async () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} />);

    const switchElement = screen.getByRole('switch');
    fireEvent.click(switchElement);

    await waitFor(() => {
      expect(screen.getByText(/Shared \(visible to reporter if identified\)/i)).toBeInTheDocument();
    });
  });

  it('calls onAddNote with correct parameters', async () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} />);

    const textarea = screen.getByPlaceholderText('Add a note about this incident...');
    const tagsInput = screen.getByPlaceholderText('Tags (optional, comma-separated)');
    const saveButton = screen.getByRole('button', { name: /Save Note/i });

    fireEvent.change(textarea, { target: { value: 'New note content' } });
    fireEvent.change(tagsInput, { target: { value: 'tag1, tag2' } });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(mockOnAddNote).toHaveBeenCalledWith(
        'New note content',
        true, // Default isPrivate
        ['tag1', 'tag2']
      );
    });
  });

  it('clears form after successful note addition', async () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} />);

    const textarea = screen.getByPlaceholderText('Add a note about this incident...');
    const tagsInput = screen.getByPlaceholderText('Tags (optional, comma-separated)');
    const saveButton = screen.getByRole('button', { name: /Save Note/i });

    fireEvent.change(textarea, { target: { value: 'New note' } });
    fireEvent.change(tagsInput, { target: { value: 'tag1' } });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(textarea).toHaveValue('');
      expect(tagsInput).toHaveValue('');
    });
  });

  it('displays notes in chronological order (newest first)', () => {
    const olderNote: IncidentNoteDto = {
      ...mockManualNote,
      id: 'n3',
      createdAt: '2025-10-15T10:00:00Z',
      content: 'Older note'
    };

    const newerNote: IncidentNoteDto = {
      ...mockManualNote,
      id: 'n4',
      createdAt: '2025-10-18T10:00:00Z',
      content: 'Newer note'
    };

    renderWithProvider(<IncidentNotesList notes={[olderNote, newerNote]} onAddNote={mockOnAddNote} />);

    const notes = screen.getAllByTestId('manual-note');
    expect(notes[0]).toHaveTextContent('Newer note');
    expect(notes[1]).toHaveTextContent('Older note');
  });

  it('shows loading state on save button when isAddingNote is true', () => {
    renderWithProvider(<IncidentNotesList notes={[]} onAddNote={mockOnAddNote} isAddingNote={true} />);
    expect(screen.getByRole('button', { name: /Saving.../i })).toBeInTheDocument();
  });
});
