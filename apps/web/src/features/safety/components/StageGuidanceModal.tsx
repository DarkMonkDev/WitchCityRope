import React, { useState } from 'react';
import {
  Modal,
  Stack,
  Text,
  Textarea,
  TextInput,
  Select,
  Button,
  Group,
  Title,
  Checkbox,
  Alert
} from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { IconInfoCircle } from '@tabler/icons-react';

type StageGuidanceVariant =
  | 'assignToGathering'
  | 'moveToReviewing'
  | 'putOnHold'
  | 'resumeFromHold'
  | 'close';

interface StageGuidanceModalProps {
  opened: boolean;
  onClose: () => void;
  onConfirm: (data: StageTransitionData) => Promise<void>;
  variant: StageGuidanceVariant;
  incidentReference?: string;
}

export interface StageTransitionData {
  note?: string;
  googleDriveLink?: string;
  holdReason?: string;
  expectedResumeDate?: Date | null;
  resumeToStatus?: 'InformationGathering' | 'ReviewingFinalReport';
  finalSummary?: string;
}

// Modal configurations for each variant
const getModalConfig = (variant: StageGuidanceVariant) => {
  switch (variant) {
    case 'assignToGathering':
      return {
        title: 'Assign to Information Gathering',
        guidanceText: 'The coordinator will now begin gathering additional information about this incident. Initial review should be complete before proceeding.',
        checklist: [
          'Initial assessment completed',
          'Coordinator assigned',
          'Google Drive folder created (Phase 1: manual checkbox only)'
        ],
        showGoogleDriveInput: false,
        showNoteField: true,
        noteLabel: 'Reason for assignment / Initial findings (Optional)',
        confirmButtonText: 'Begin Information Gathering',
        confirmButtonColor: 'purple'
      };

    case 'moveToReviewing':
      return {
        title: 'Move to Reviewing Final Report',
        guidanceText: 'The investigation is complete and the coordinator is preparing the final report. All information gathering should be finished before this stage.',
        checklist: [
          'All parties interviewed',
          'Evidence collected',
          'Final report drafted in Google Drive (Phase 1: manual checkbox)'
        ],
        showGoogleDriveInput: false,
        showNoteField: true,
        noteLabel: 'Summary of investigation findings (Optional)',
        confirmButtonText: 'Move to Final Review',
        confirmButtonColor: 'purple'
      };

    case 'putOnHold':
      return {
        title: 'Put Incident On Hold',
        guidanceText: 'This incident will remain on hold until additional information is available or external processes complete.',
        checklist: [
          'Reason for hold documented',
          'Expected resume date identified'
        ],
        showHoldReasonField: true,
        showExpectedResumeDateField: true,
        confirmButtonText: 'Put On Hold',
        confirmButtonColor: 'orange'
      };

    case 'resumeFromHold':
      return {
        title: 'Resume Investigation',
        guidanceText: 'Investigation will resume. Review notes since hold to ensure all blockers are resolved.',
        checklist: [
          'Circumstances resolved',
          'Ready to continue investigation'
        ],
        showResumeToDropdown: true,
        showNoteField: true,
        noteLabel: 'Reason for resuming (Optional)',
        confirmButtonText: 'Resume Investigation',
        confirmButtonColor: 'purple'
      };

    case 'close':
      return {
        title: 'Close Incident',
        guidanceText: 'Once closed, this incident will move to archived status. Ensure all documentation is complete before closing.',
        checklist: [
          'Final report completed',
          'Actions taken documented',
          'All parties notified (if applicable)'
        ],
        showFinalSummaryField: true,
        showNoteField: false,
        confirmButtonText: 'Close Incident',
        confirmButtonColor: 'green'
      };

    default:
      return {
        title: 'Status Change',
        guidanceText: '',
        checklist: [],
        confirmButtonText: 'Confirm',
        confirmButtonColor: 'blue'
      };
  }
};

export const StageGuidanceModal: React.FC<StageGuidanceModalProps> = ({
  opened,
  onClose,
  onConfirm,
  variant,
  incidentReference
}) => {
  const config = getModalConfig(variant);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [note, setNote] = useState('');
  const [googleDriveLink, setGoogleDriveLink] = useState('');
  const [holdReason, setHoldReason] = useState('');
  const [expectedResumeDate, setExpectedResumeDate] = useState<Date | null>(null);
  const [resumeToStatus, setResumeToStatus] = useState<'InformationGathering' | 'ReviewingFinalReport' | null>(null);
  const [finalSummary, setFinalSummary] = useState('');

  // Checklist state (informative only - not enforced)
  const [checklistItems, setChecklistItems] = useState<boolean[]>(
    config.checklist.map(() => false)
  );

  const handleChecklistToggle = (index: number) => {
    setChecklistItems(prev => {
      const newItems = [...prev];
      newItems[index] = !newItems[index];
      return newItems;
    });
  };

  const handleConfirm = async () => {
    setIsSubmitting(true);
    try {
      const data: StageTransitionData = {
        note: note.trim() || undefined,
        googleDriveLink: googleDriveLink.trim() || undefined,
        holdReason: holdReason.trim() || undefined,
        expectedResumeDate: expectedResumeDate || undefined,
        resumeToStatus: resumeToStatus || undefined,
        finalSummary: finalSummary.trim() || undefined
      };

      await onConfirm(data);
      handleClose();
    } catch (error) {
      // Error handled by parent
      console.error('Stage transition failed:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      // Reset all form fields
      setNote('');
      setGoogleDriveLink('');
      setHoldReason('');
      setExpectedResumeDate(null);
      setResumeToStatus(null);
      setFinalSummary('');
      setChecklistItems(config.checklist.map(() => false));
      onClose();
    }
  };

  // Validation: some variants require specific fields
  const isValid = () => {
    if (variant === 'resumeFromHold' && !resumeToStatus) {
      return false;
    }
    if (variant === 'close' && !finalSummary.trim()) {
      return false;
    }
    return true;
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          {config.title}
        </Title>
      }
      centered
      size="md"
      data-testid={`stage-guidance-modal-${variant}`}
    >
      <Stack gap="md">
        {/* Guidance Text */}
        <Text size="sm">{config.guidanceText}</Text>

        {/* Checklist (NOT enforced - informative only) */}
        {config.checklist.length > 0 && (
          <Stack gap="xs">
            <Text size="sm" fw={600}>Recommended Actions:</Text>
            {config.checklist.map((item, index) => (
              <Group gap="xs" key={index}>
                <Checkbox
                  size="xs"
                  checked={checklistItems[index]}
                  onChange={() => handleChecklistToggle(index)}
                  data-testid={`checklist-item-${index}`}
                />
                <Text size="sm">{item}</Text>
              </Group>
            ))}
          </Stack>
        )}

        {/* Alert: Soft enforcement - checklists are guidance only */}
        <Alert color="blue" icon={<IconInfoCircle />}>
          <Text size="xs">
            These recommendations are guidance only. You may proceed without completing all items.
          </Text>
        </Alert>

        {/* Google Drive Link Field */}
        {config.showGoogleDriveInput && (
          <TextInput
            label="Google Drive Link (Optional)"
            placeholder="https://drive.google.com/..."
            value={googleDriveLink}
            onChange={(e) => setGoogleDriveLink(e.currentTarget.value)}
            data-testid="google-drive-link-input"
          />
        )}

        {/* Hold Reason Field */}
        {config.showHoldReasonField && (
          <Textarea
            label="Reason for Hold (Optional)"
            placeholder="Enter the reason for putting this incident on hold..."
            value={holdReason}
            onChange={(e) => setHoldReason(e.currentTarget.value)}
            minRows={3}
            data-testid="hold-reason-textarea"
            styles={{
              input: {
                borderRadius: '8px',
                border: '1px solid #E0E0E0',
              }
            }}
          />
        )}

        {/* Expected Resume Date Field */}
        {config.showExpectedResumeDateField && (
          <DatePickerInput
            label="Expected Resume Date (Optional)"
            placeholder="Select date..."
            value={expectedResumeDate}
            onChange={setExpectedResumeDate}
            data-testid="expected-resume-date-input"
            minDate={new Date()}
          />
        )}

        {/* Resume To Status Dropdown */}
        {config.showResumeToDropdown && (
          <Select
            label="Resume To Stage (Required)"
            placeholder="Select stage to resume to..."
            data={[
              { value: 'InformationGathering', label: 'Information Gathering' },
              { value: 'ReviewingFinalReport', label: 'Reviewing Final Report' }
            ]}
            value={resumeToStatus}
            onChange={(value) => setResumeToStatus(value as 'InformationGathering' | 'ReviewingFinalReport')}
            required
            data-testid="resume-to-status-select"
            styles={{
              input: {
                fontSize: '16px',
                height: '42px',
                borderRadius: '8px'
              }
            }}
          />
        )}

        {/* Final Summary Field */}
        {config.showFinalSummaryField && (
          <Textarea
            label="Closure Summary and Actions Taken (Required)"
            placeholder="Provide a final summary of the incident resolution..."
            value={finalSummary}
            onChange={(e) => setFinalSummary(e.currentTarget.value)}
            minRows={4}
            required
            data-testid="final-summary-textarea"
            styles={{
              input: {
                borderRadius: '8px',
                border: '1px solid #E0E0E0',
              }
            }}
          />
        )}

        {/* Optional Note Field */}
        {config.showNoteField && (
          <Textarea
            label={config.noteLabel || 'Note (Optional)'}
            placeholder="Add a note about this transition..."
            value={note}
            onChange={(e) => setNote(e.currentTarget.value)}
            minRows={3}
            data-testid="transition-note-textarea"
            styles={{
              input: {
                borderRadius: '8px',
                border: '1px solid #E0E0E0',
              }
            }}
          />
        )}

        {/* Actions */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
            data-testid="stage-guidance-cancel-button"
            styles={{
              root: {
                minHeight: 40,
                height: 'auto',
                padding: '10px 20px',
                lineHeight: 1.4
              }
            }}
          >
            Cancel
          </Button>
          <Button
            color={config.confirmButtonColor}
            onClick={handleConfirm}
            loading={isSubmitting}
            disabled={!isValid()}
            data-testid="stage-guidance-confirm-button"
            styles={{
              root: {
                minHeight: 40,
                height: 'auto',
                padding: '10px 20px',
                lineHeight: 1.4
              }
            }}
          >
            {config.confirmButtonText}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
