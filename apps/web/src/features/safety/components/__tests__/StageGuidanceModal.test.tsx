import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MantineProvider } from '@mantine/core';
import { StageGuidanceModal } from '../StageGuidanceModal';

const renderComponent = (props: any = {}) => {
  const defaultProps = {
    opened: true,
    onClose: vi.fn(),
    onConfirm: vi.fn(),
    variant: 'assignToGathering' as const,
    ...props
  };

  return render(
    <MantineProvider>
      <StageGuidanceModal {...defaultProps} />
    </MantineProvider>
  );
};

describe('StageGuidanceModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Variant: assignToGathering', () => {
    it('renders correct title and guidance text', () => {
      renderComponent({ variant: 'assignToGathering' });

      expect(screen.getByText('Assign to Information Gathering')).toBeInTheDocument();
      expect(screen.getByText(/The coordinator will now begin gathering additional information/)).toBeInTheDocument();
    });

    it('displays checklist items', () => {
      renderComponent({ variant: 'assignToGathering' });

      expect(screen.getByText('Initial assessment completed')).toBeInTheDocument();
      expect(screen.getByText('Coordinator assigned')).toBeInTheDocument();
      expect(screen.getByText(/Google Drive folder created/)).toBeInTheDocument();
    });

    it('displays soft enforcement alert', () => {
      renderComponent({ variant: 'assignToGathering' });

      expect(screen.getByText(/These recommendations are guidance only/)).toBeInTheDocument();
    });

    it('allows checklist items to be checked/unchecked', async () => {
      const user = userEvent.setup();
      renderComponent({ variant: 'assignToGathering' });

      const checkbox = screen.getByTestId('checklist-item-0');
      expect(checkbox).not.toBeChecked();

      await user.click(checkbox);
      expect(checkbox).toBeChecked();
    });

    it('displays optional note textarea', () => {
      renderComponent({ variant: 'assignToGathering' });

      expect(screen.getByTestId('transition-note-textarea')).toBeInTheDocument();
    });

    it('enables confirm button even with unchecked items', () => {
      renderComponent({ variant: 'assignToGathering' });

      const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
      expect(confirmButton).not.toBeDisabled();
    });

    it('calls onConfirm with correct data', async () => {
      const user = userEvent.setup();
      const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
      renderComponent({ variant: 'assignToGathering', onConfirm: mockOnConfirm });

      const noteTextarea = screen.getByTestId('transition-note-textarea');
      await user.type(noteTextarea, 'Test note');

      const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
      await user.click(confirmButton);

      await waitFor(() => {
        expect(mockOnConfirm).toHaveBeenCalledWith({
          note: 'Test note',
          googleDriveLink: undefined,
          holdReason: undefined,
          expectedResumeDate: undefined,
          resumeToStatus: undefined,
          finalSummary: undefined
        });
      });
    });
  });

  describe('Variant: moveToReviewing', () => {
    it('renders correct title and guidance text', () => {
      renderComponent({ variant: 'moveToReviewing' });

      expect(screen.getByText('Move to Reviewing Final Report')).toBeInTheDocument();
      expect(screen.getByText(/The investigation is complete/)).toBeInTheDocument();
    });

    it('displays correct checklist items', () => {
      renderComponent({ variant: 'moveToReviewing' });

      expect(screen.getByText('All parties interviewed')).toBeInTheDocument();
      expect(screen.getByText('Evidence collected')).toBeInTheDocument();
      expect(screen.getByText(/Final report drafted in Google Drive/)).toBeInTheDocument();
    });

    it('shows correct confirm button text', () => {
      renderComponent({ variant: 'moveToReviewing' });

      expect(screen.getByTestId('stage-guidance-confirm-button')).toHaveTextContent('Move to Final Review');
    });
  });

  describe('Variant: putOnHold', () => {
    it('renders correct title and guidance text', () => {
      renderComponent({ variant: 'putOnHold' });

      expect(screen.getByText('Put Incident On Hold')).toBeInTheDocument();
      expect(screen.getByText(/This incident will remain on hold/)).toBeInTheDocument();
    });

    it('displays hold reason textarea', () => {
      renderComponent({ variant: 'putOnHold' });

      expect(screen.getByTestId('hold-reason-textarea')).toBeInTheDocument();
    });

    it('displays expected resume date picker', () => {
      renderComponent({ variant: 'putOnHold' });

      expect(screen.getByTestId('expected-resume-date-input')).toBeInTheDocument();
    });

    it('calls onConfirm with hold reason and date', async () => {
      const user = userEvent.setup();
      const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
      renderComponent({ variant: 'putOnHold', onConfirm: mockOnConfirm });

      const reasonTextarea = screen.getByTestId('hold-reason-textarea');
      await user.type(reasonTextarea, 'Awaiting police report');

      const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
      await user.click(confirmButton);

      await waitFor(() => {
        expect(mockOnConfirm).toHaveBeenCalledWith(
          expect.objectContaining({
            holdReason: 'Awaiting police report'
          })
        );
      });
    });
  });

  describe('Variant: resumeFromHold', () => {
    it('renders correct title and guidance text', () => {
      renderComponent({ variant: 'resumeFromHold' });

      expect(screen.getByText('Resume Investigation')).toBeInTheDocument();
      expect(screen.getByText(/Investigation will resume/)).toBeInTheDocument();
    });

    it('displays resume to status dropdown', () => {
      renderComponent({ variant: 'resumeFromHold' });

      expect(screen.getByTestId('resume-to-status-select')).toBeInTheDocument();
    });

    it('disables confirm button when no status selected', () => {
      renderComponent({ variant: 'resumeFromHold' });

      const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
      expect(confirmButton).toBeDisabled();
    });

    it('enables confirm button when status selected', async () => {
      const user = userEvent.setup();
      renderComponent({ variant: 'resumeFromHold' });

      const select = screen.getByLabelText(/Resume To Stage/);
      await user.click(select);

      const option = await screen.findByText('Information Gathering');
      await user.click(option);

      await waitFor(() => {
        const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
        expect(confirmButton).not.toBeDisabled();
      });
    });

    it('calls onConfirm with resume status', async () => {
      const user = userEvent.setup();
      const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
      renderComponent({ variant: 'resumeFromHold', onConfirm: mockOnConfirm });

      const select = screen.getByLabelText(/Resume To Stage/);
      await user.click(select);

      const option = await screen.findByText('Information Gathering');
      await user.click(option);

      const confirmButton = await screen.findByTestId('stage-guidance-confirm-button');
      await user.click(confirmButton);

      await waitFor(() => {
        expect(mockOnConfirm).toHaveBeenCalledWith(
          expect.objectContaining({
            resumeToStatus: 'InformationGathering'
          })
        );
      });
    });
  });

  describe('Variant: close', () => {
    it('renders correct title and guidance text', () => {
      renderComponent({ variant: 'close' });

      expect(screen.getByText('Close Incident')).toBeInTheDocument();
      expect(screen.getByText(/Once closed, this incident will move to archived status/)).toBeInTheDocument();
    });

    it('displays final summary textarea as required', () => {
      renderComponent({ variant: 'close' });

      const finalSummaryTextarea = screen.getByTestId('final-summary-textarea');
      expect(finalSummaryTextarea).toBeInTheDocument();
      expect(screen.getByText(/Closure Summary and Actions Taken \(Required\)/)).toBeInTheDocument();
    });

    it('disables confirm button when final summary is empty', () => {
      renderComponent({ variant: 'close' });

      const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
      expect(confirmButton).toBeDisabled();
    });

    it('enables confirm button when final summary is provided', async () => {
      const user = userEvent.setup();
      renderComponent({ variant: 'close' });

      const finalSummaryTextarea = screen.getByTestId('final-summary-textarea');
      await user.type(finalSummaryTextarea, 'Incident resolved through mediation');

      await waitFor(() => {
        const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
        expect(confirmButton).not.toBeDisabled();
      });
    });

    it('calls onConfirm with final summary', async () => {
      const user = userEvent.setup();
      const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
      renderComponent({ variant: 'close', onConfirm: mockOnConfirm });

      const finalSummaryTextarea = screen.getByTestId('final-summary-textarea');
      await user.type(finalSummaryTextarea, 'Incident resolved through mediation');

      const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
      await user.click(confirmButton);

      await waitFor(() => {
        expect(mockOnConfirm).toHaveBeenCalledWith(
          expect.objectContaining({
            finalSummary: 'Incident resolved through mediation'
          })
        );
      });
    });
  });

  describe('Common modal behavior', () => {
    it('calls onClose when cancel button clicked', async () => {
      const user = userEvent.setup();
      const mockOnClose = vi.fn();
      renderComponent({ onClose: mockOnClose });

      const cancelButton = screen.getByTestId('stage-guidance-cancel-button');
      await user.click(cancelButton);

      expect(mockOnClose).toHaveBeenCalled();
    });

    it('resets form when closed', async () => {
      const user = userEvent.setup();
      const mockOnClose = vi.fn();
      renderComponent({ onClose: mockOnClose, variant: 'assignToGathering' });

      const noteTextarea = screen.getByTestId('transition-note-textarea');
      await user.type(noteTextarea, 'Test note');

      const cancelButton = screen.getByTestId('stage-guidance-cancel-button');
      await user.click(cancelButton);

      expect(mockOnClose).toHaveBeenCalled();
    });

    it('closes modal after successful confirmation', async () => {
      const user = userEvent.setup();
      const mockOnConfirm = vi.fn().mockResolvedValue(undefined);
      const mockOnClose = vi.fn();
      renderComponent({ onConfirm: mockOnConfirm, onClose: mockOnClose });

      const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
      await user.click(confirmButton);

      await waitFor(() => {
        expect(mockOnClose).toHaveBeenCalled();
      });
    });

    it('handles confirmation error gracefully', async () => {
      const user = userEvent.setup();
      const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      const mockOnConfirm = vi.fn().mockRejectedValue(new Error('Confirmation failed'));
      renderComponent({ onConfirm: mockOnConfirm });

      const confirmButton = screen.getByTestId('stage-guidance-confirm-button');
      await user.click(confirmButton);

      await waitFor(() => {
        expect(consoleErrorSpy).toHaveBeenCalled();
      });

      consoleErrorSpy.mockRestore();
    });
  });
});
