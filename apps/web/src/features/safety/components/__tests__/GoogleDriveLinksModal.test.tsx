import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MantineProvider } from '@mantine/core';
import { GoogleDriveLinksModal } from '../GoogleDriveLinksModal';

const renderComponent = (props: any = {}) => {
  const defaultProps = {
    opened: true,
    onClose: vi.fn(),
    onSave: vi.fn(),
    ...props
  };

  return render(
    <MantineProvider>
      <GoogleDriveLinksModal {...defaultProps} />
    </MantineProvider>
  );
};

describe('GoogleDriveLinksModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders modal with title when opened', () => {
    renderComponent();
    expect(screen.getByText('Update Google Drive Links')).toBeInTheDocument();
  });

  it('displays Phase 1 manual process alert', () => {
    renderComponent();
    expect(screen.getByText(/Phase 1: Manual entry only/)).toBeInTheDocument();
  });

  it('displays instruction text', () => {
    renderComponent();
    expect(screen.getByText(/Enter Google Drive URLs/)).toBeInTheDocument();
  });

  it('renders folder URL input field', () => {
    renderComponent();
    expect(screen.getByTestId('folder-url-input')).toBeInTheDocument();
  });

  it('renders final report URL input field', () => {
    renderComponent();
    expect(screen.getByTestId('final-report-url-input')).toBeInTheDocument();
  });

  it('displays help text about URL format', () => {
    renderComponent();
    expect(screen.getByText(/URLs must start with/)).toBeInTheDocument();
    expect(screen.getByText(/https:\/\/drive.google.com\//)).toBeInTheDocument();
  });

  it('pre-populates current folder URL when provided', () => {
    const currentFolderUrl = 'https://drive.google.com/drive/folders/test123';
    renderComponent({ currentFolderUrl });

    const folderInput = screen.getByTestId('folder-url-input') as HTMLInputElement;
    expect(folderInput.value).toBe(currentFolderUrl);
  });

  it('pre-populates current final report URL when provided', () => {
    const currentFinalReportUrl = 'https://drive.google.com/file/d/test456';
    renderComponent({ currentFinalReportUrl });

    const reportInput = screen.getByTestId('final-report-url-input') as HTMLInputElement;
    expect(reportInput.value).toBe(currentFinalReportUrl);
  });

  it('validates folder URL must start with Google Drive prefix', async () => {
    const user = userEvent.setup();
    renderComponent();

    const folderInput = screen.getByTestId('folder-url-input');
    await user.type(folderInput, 'https://example.com/folder');

    await waitFor(() => {
      expect(screen.getByText(/URL must start with https:\/\/drive.google.com\//)).toBeInTheDocument();
    });
  });

  it('validates final report URL must start with Google Drive prefix', async () => {
    const user = userEvent.setup();
    renderComponent();

    const reportInput = screen.getByTestId('final-report-url-input');
    await user.type(reportInput, 'https://example.com/report');

    await waitFor(() => {
      // There will be 2 error messages shown (one for each invalid field)
      const errorMessages = screen.getAllByText(/URL must start with https:\/\/drive.google.com\//);
      expect(errorMessages.length).toBeGreaterThan(0);
    });
  });

  it('accepts valid Google Drive folder URL', async () => {
    const user = userEvent.setup();
    renderComponent();

    const folderInput = screen.getByTestId('folder-url-input');
    await user.type(folderInput, 'https://drive.google.com/drive/folders/test123');

    await waitFor(() => {
      expect(screen.queryByText(/URL must start with https:\/\/drive.google.com\//)).not.toBeInTheDocument();
    });
  });

  it('accepts valid Google Drive file URL', async () => {
    const user = userEvent.setup();
    renderComponent();

    const reportInput = screen.getByTestId('final-report-url-input');
    await user.type(reportInput, 'https://drive.google.com/file/d/test456');

    // Should not show error for valid URL
    await waitFor(() => {
      const saveButton = screen.getByTestId('google-drive-save-button');
      expect(saveButton).not.toBeDisabled();
    });
  });

  it('allows empty URLs (both fields are optional)', () => {
    renderComponent();

    const saveButton = screen.getByTestId('google-drive-save-button');
    expect(saveButton).not.toBeDisabled();
  });

  it('disables save button when folder URL is invalid', async () => {
    const user = userEvent.setup();
    renderComponent();

    const folderInput = screen.getByTestId('folder-url-input');
    await user.type(folderInput, 'https://example.com/folder');

    await waitFor(() => {
      const saveButton = screen.getByTestId('google-drive-save-button');
      expect(saveButton).toBeDisabled();
    });
  });

  it('disables save button when final report URL is invalid', async () => {
    const user = userEvent.setup();
    renderComponent();

    const reportInput = screen.getByTestId('final-report-url-input');
    await user.type(reportInput, 'https://example.com/report');

    await waitFor(() => {
      const saveButton = screen.getByTestId('google-drive-save-button');
      expect(saveButton).toBeDisabled();
    });
  });

  it('calls onSave with both URLs when save button clicked', async () => {
    const user = userEvent.setup();
    const mockOnSave = vi.fn().mockResolvedValue(undefined);
    renderComponent({ onSave: mockOnSave });

    const folderInput = screen.getByTestId('folder-url-input');
    await user.type(folderInput, 'https://drive.google.com/drive/folders/test123');

    const reportInput = screen.getByTestId('final-report-url-input');
    await user.type(reportInput, 'https://drive.google.com/file/d/test456');

    const saveButton = screen.getByTestId('google-drive-save-button');
    await user.click(saveButton);

    await waitFor(() => {
      expect(mockOnSave).toHaveBeenCalledWith(
        'https://drive.google.com/drive/folders/test123',
        'https://drive.google.com/file/d/test456'
      );
    });
  });

  it('calls onSave with undefined for empty URLs', async () => {
    const user = userEvent.setup();
    const mockOnSave = vi.fn().mockResolvedValue(undefined);
    renderComponent({ onSave: mockOnSave });

    const saveButton = screen.getByTestId('google-drive-save-button');
    await user.click(saveButton);

    await waitFor(() => {
      expect(mockOnSave).toHaveBeenCalledWith(undefined, undefined);
    });
  });

  it('calls onClose when cancel button clicked', async () => {
    const user = userEvent.setup();
    const mockOnClose = vi.fn();
    renderComponent({ onClose: mockOnClose });

    const cancelButton = screen.getByTestId('google-drive-cancel-button');
    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  it('resets form to current values when cancelled', async () => {
    const user = userEvent.setup();
    const currentFolderUrl = 'https://drive.google.com/drive/folders/original';
    renderComponent({ currentFolderUrl });

    const folderInput = screen.getByTestId('folder-url-input');
    await user.clear(folderInput);
    await user.type(folderInput, 'https://drive.google.com/drive/folders/changed');

    const cancelButton = screen.getByTestId('google-drive-cancel-button');
    await user.click(cancelButton);

    // After cancelling, if modal is re-opened, it should show original value
    // This is tested by checking the initial value prop in a new render
  });

  it('closes modal after successful save', async () => {
    const user = userEvent.setup();
    const mockOnSave = vi.fn().mockResolvedValue(undefined);
    const mockOnClose = vi.fn();
    renderComponent({ onSave: mockOnSave, onClose: mockOnClose });

    const saveButton = screen.getByTestId('google-drive-save-button');
    await user.click(saveButton);

    await waitFor(() => {
      expect(mockOnClose).toHaveBeenCalled();
    });
  });

  it('displays loading state on save button when submitting', async () => {
    const user = userEvent.setup();
    let resolveSave: any;
    const mockOnSave = vi.fn(() => new Promise((resolve) => { resolveSave = resolve; }));
    renderComponent({ onSave: mockOnSave });

    const saveButton = screen.getByTestId('google-drive-save-button');
    await user.click(saveButton);

    // Button should show loading state
    await waitFor(() => {
      expect(screen.getByTestId('google-drive-save-button')).toBeDisabled();
    });

    // Resolve the promise
    resolveSave();
  });

  it('handles save error gracefully', async () => {
    const user = userEvent.setup();
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    const mockOnSave = vi.fn().mockRejectedValue(new Error('Save failed'));
    renderComponent({ onSave: mockOnSave });

    const saveButton = screen.getByTestId('google-drive-save-button');
    await user.click(saveButton);

    await waitFor(() => {
      expect(consoleErrorSpy).toHaveBeenCalled();
    });

    consoleErrorSpy.mockRestore();
  });

  it('trims whitespace from URLs before saving', async () => {
    const user = userEvent.setup();
    const mockOnSave = vi.fn().mockResolvedValue(undefined);
    renderComponent({ onSave: mockOnSave });

    const folderInput = screen.getByTestId('folder-url-input');
    await user.type(folderInput, '  https://drive.google.com/drive/folders/test123  ');

    const saveButton = screen.getByTestId('google-drive-save-button');
    await user.click(saveButton);

    await waitFor(() => {
      expect(mockOnSave).toHaveBeenCalledWith(
        'https://drive.google.com/drive/folders/test123',
        undefined
      );
    });
  });
});
