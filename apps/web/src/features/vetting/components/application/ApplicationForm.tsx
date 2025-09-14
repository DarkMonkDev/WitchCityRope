// Multi-step vetting application form component
import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Stepper,
  Button,
  Group,
  Text,
  Paper,
  Alert,
  LoadingOverlay,
  Stack,
  Title,
  Progress,
  Divider
} from '@mantine/core';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { IconAlertCircle, IconCheck, IconShieldCheck } from '@tabler/icons-react';
import { z } from 'zod';
import { useVettingApplication } from '../../hooks/useVettingApplication';
import { PersonalInfoStep } from './PersonalInfoStep';
import { ExperienceStep } from './ExperienceStep';
import { CommunityStep } from './CommunityStep';
import { ReferencesStep } from './ReferencesStep';
import { ReviewStep } from './ReviewStep';
import type { ApplicationFormState } from '../../types/api-types';
import { transformFormStateToApiRequest } from '../../types/api-types';
import { TOUCH_TARGETS } from '../../types/vetting.types';

// Validation schema aligned with API CreateApplicationRequest structure
const applicationSchema = z.object({
  // Personal Information - matches API exactly
  fullName: z.string().min(2, 'Full name must be at least 2 characters'),
  sceneName: z.string().min(2, 'Scene name must be at least 2 characters'),
  pronouns: z.string().optional(),
  email: z.string().email('Please enter a valid email address'),
  phone: z.string().optional(),
  
  // Experience & Knowledge - matches API property names
  experienceLevel: z.number().min(1).max(4),
  yearsExperience: z.number().min(0).max(50),
  experienceDescription: z.string().min(50, 'Please provide at least 50 characters'),
  safetyKnowledge: z.string().min(30, 'Please describe your safety knowledge'),
  consentUnderstanding: z.string().min(30, 'Please describe your understanding of consent'),
  
  // Community Understanding - matches API property names
  whyJoinCommunity: z.string().min(50, 'Please tell us why you want to join (minimum 50 characters)'),
  skillsInterests: z.array(z.string()).min(1, 'Please select at least one skill or interest'),
  expectationsGoals: z.string().min(30, 'Please describe your expectations'),
  agreesToGuidelines: z.boolean().refine(val => val === true, 'You must agree to community guidelines'),
  
  // References - UI convenience structure (will be transformed to array)
  reference1: z.object({
    name: z.string().min(2, 'Reference name is required'),
    email: z.string().email('Valid email is required'),
    relationship: z.string().min(5, 'Please describe your relationship')
  }),
  reference2: z.object({
    name: z.string().min(2, 'Reference name is required'),
    email: z.string().email('Valid email is required'),
    relationship: z.string().min(5, 'Please describe your relationship')
  }),
  reference3: z.object({
    name: z.string().optional(),
    email: z.string().email().optional().or(z.literal('')),
    relationship: z.string().optional()
  }),
  
  // Review & Submit - matches API exactly
  agreesToTerms: z.boolean().refine(val => val === true, 'You must agree to terms and conditions'),
  isAnonymous: z.boolean(),
  consentToContact: z.boolean()
});

interface ApplicationFormProps {
  onSubmissionComplete?: (applicationNumber: string, statusUrl: string) => void;
  className?: string;
}

export const ApplicationForm: React.FC<ApplicationFormProps> = ({
  onSubmissionComplete,
  className
}) => {
  const [currentStep, setCurrentStep] = useState(0);
  const [isAutoSaving, setIsAutoSaving] = useState(false);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);

  const {
    submitApplication,
    isSubmitting,
    submissionResult,
    autoSaveDraft,
    draftData,
    isDraftLoading,
    clearDraft
  } = useVettingApplication();

  // Initialize form with flat structure matching API
  const form = useForm({
    resolver: zodResolver(applicationSchema) as any,
    defaultValues: {
      // Personal Information
      fullName: '',
      sceneName: '',
      pronouns: '',
      email: '',
      phone: '',
      
      // Experience & Knowledge
      experienceLevel: 1,
      yearsExperience: 0,
      experienceDescription: '',
      safetyKnowledge: '',
      consentUnderstanding: '',
      
      // Community Understanding
      whyJoinCommunity: '',
      skillsInterests: [],
      expectationsGoals: '',
      agreesToGuidelines: false,
      
      // References
      reference1: { name: '', email: '', relationship: '' },
      reference2: { name: '', email: '', relationship: '' },
      reference3: { name: '', email: '', relationship: '' },
      
      // Review & Submit
      agreesToTerms: false,
      isAnonymous: false,
      consentToContact: true
    },
    mode: 'onChange'
  });

  // Load draft data on component mount
  useEffect(() => {
    if (draftData && !isDraftLoading) {
      // Type assertion for draft data since it comes from API as unknown
      const draft = draftData as any;
      
      form.reset({
        // Personal Information
        fullName: draft.fullName || '',
        sceneName: draft.sceneName || '',
        pronouns: draft.pronouns || '',
        email: draft.email || '',
        phone: draft.phone || '',
        
        // Experience & Knowledge
        experienceLevel: draft.experienceLevel || 1,
        yearsExperience: draft.yearsExperience || 0,
        experienceDescription: draft.experienceDescription || '',
        safetyKnowledge: draft.safetyKnowledge || '',
        consentUnderstanding: draft.consentUnderstanding || '',
        
        // Community Understanding
        whyJoinCommunity: draft.whyJoinCommunity || '',
        skillsInterests: draft.skillsInterests || [],
        expectationsGoals: draft.expectationsGoals || '',
        agreesToGuidelines: draft.agreesToGuidelines || false,
        
        // References
        reference1: draft.references?.[0] || { name: '', email: '', relationship: '' },
        reference2: draft.references?.[1] || { name: '', email: '', relationship: '' },
        reference3: draft.references?.[2] || { name: '', email: '', relationship: '' },
        
        // Review & Submit
        agreesToTerms: draft.agreesToTerms || false,
        isAnonymous: draft.isAnonymous || false,
        consentToContact: draft.consentToContact || true
      });
    }
  }, [draftData, isDraftLoading, form]);

  // Auto-save functionality
  const handleAutoSave = useCallback(async () => {
    const formData = form.getValues();
    const email = formData.email;

    if (!email) return;

    setIsAutoSaving(true);
    try {
      // Transform form state to API format before saving
      const apiData = transformFormStateToApiRequest(formData);
      await autoSaveDraft(apiData, email);
      setLastSaved(new Date());
    } catch (error) {
      console.error('Auto-save failed:', error);
    } finally {
      setIsAutoSaving(false);
    }
  }, [form, autoSaveDraft]);

  // Auto-save on form changes (debounced)
  useEffect(() => {
    const subscription = form.watch(() => {
      const timeoutId = setTimeout(handleAutoSave, 2000); // 2 second debounce
      return () => clearTimeout(timeoutId);
    });

    return () => subscription.unsubscribe();
  }, [form, handleAutoSave]);

  // Step navigation handlers
  const nextStep = async () => {
    const isStepValid = await validateCurrentStep();
    if (isStepValid) {
      setCurrentStep((prev) => Math.min(prev + 1, 4));
      await handleAutoSave(); // Save progress when moving forward
    }
  };

  const prevStep = () => {
    setCurrentStep((prev) => Math.max(prev - 1, 0));
  };

  // Validate current step
  const validateCurrentStep = async (): Promise<boolean> => {
    switch (currentStep) {
      case 0: // Personal Info - flat field names
        return form.trigger(['fullName', 'sceneName', 'email']);
      case 1: // Experience - flat field names
        return form.trigger([
          'experienceLevel',
          'yearsExperience', 
          'experienceDescription',
          'safetyKnowledge',
          'consentUnderstanding'
        ]);
      case 2: // Community - flat field names
        return form.trigger([
          'whyJoinCommunity',
          'skillsInterests',
          'expectationsGoals',
          'agreesToGuidelines'
        ]);
      case 3: // References - flat field names
        return form.trigger([
          'reference1.name',
          'reference1.email',
          'reference1.relationship',
          'reference2.name',
          'reference2.email',
          'reference2.relationship'
        ]);
      case 4: // Review - flat field names
        return form.trigger(['agreesToTerms']);
      default:
        return true;
    }
  };

  // Handle form submission
  const handleSubmit = async (data: ApplicationFormState) => {
    try {
      submitApplication(data);
    } catch (error) {
      console.error('Submission failed:', error);
    }
  };

  // Handle successful submission
  useEffect(() => {
    if (submissionResult) {
      clearDraft(); // Clear draft after successful submission
      // Type assertion for submission result
      const result = submissionResult as any;
      onSubmissionComplete?.(
        result.applicationNumber,
        result.statusCheckUrl
      );
    }
  }, [submissionResult, clearDraft, onSubmissionComplete]);

  // Step components - cast form to any to avoid nested vs flat structure conflicts
  const stepComponents = [
    <PersonalInfoStep key="personal" form={form as any} />,
    <ExperienceStep key="experience" form={form as any} />,
    <CommunityStep key="community" form={form as any} />,
    <ReferencesStep key="references" form={form as any} />,
    <ReviewStep key="review" form={form as any} />
  ];

  const stepLabels = [
    'Personal Information',
    'Experience & Knowledge',
    'Community Understanding',
    'References',
    'Review & Submit'
  ];

  return (
    <Box className={className} pos="relative">
      <LoadingOverlay visible={isDraftLoading} />
      
      {/* Header */}
      <Paper p="xl" shadow="sm" mb="lg">
        <Stack gap="md">
          <Title order={1} size="h2" ta="center" c="wcr.7">
            Join Our Community
          </Title>
          <Text ta="center" c="dimmed" size="lg">
            Complete our vetting process to become a member of WitchCityRope
          </Text>
          
          {/* Progress indicators */}
          <Group justify="apart" align="center">
            <Text size="sm" c="dimmed">
              Step {currentStep + 1} of {stepLabels.length}
            </Text>
            {(isAutoSaving || lastSaved) && (
              <Group gap="xs">
                {isAutoSaving ? (
                  <Text size="xs" c="blue">Saving draft...</Text>
                ) : lastSaved && (
                  <Group gap={4}>
                    <IconCheck size={14} color="green" />
                    <Text size="xs" c="green">
                      Last saved: {lastSaved.toLocaleTimeString()}
                    </Text>
                  </Group>
                )}
              </Group>
            )}
          </Group>
          
          <Progress value={((currentStep + 1) / stepLabels.length) * 100} color="wcr.7" />
        </Stack>
      </Paper>

      {/* Privacy Notice */}
      <Alert 
        icon={<IconShieldCheck />}
        color="blue"
        title="Privacy & Data Protection"
        mb="lg"
      >
        All personal information is encrypted and only accessible to approved vetting team members. 
        Your data will never be shared outside the review process, and you can choose to apply anonymously.
      </Alert>

      <form onSubmit={form.handleSubmit(handleSubmit as any)}>
        {/* Stepper */}
        {React.createElement(Stepper as any, {
          active: currentStep,
          onStepClick: setCurrentStep,
          allowNextStepsSelect: false,
          breakpoint: "sm",
          iconSize: 40,
          styles: () => ({
            stepIcon: {
              borderWidth: 2
            },
            stepLabel: {
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 600
            }
          }),
          mb: "xl"
        },
          stepLabels.map((label, index) => (
            React.createElement(Stepper.Step, { key: index, label: label })
          ))
        )}

        {/* Step Content */}
        <Paper p="xl" shadow="sm" mb="xl">
          {stepComponents[currentStep]}
        </Paper>

        {/* Navigation */}
        <Paper p="lg" shadow="sm">
          <Group justify="apart">
            <Button
              variant="outline"
              color="wcr.7"
              onClick={prevStep}
              disabled={currentStep === 0}
              size="lg"
              style={{ minHeight: TOUCH_TARGETS.BUTTON_HEIGHT }}
            >
              Back
            </Button>

            <Group>
              {currentStep < stepLabels.length - 1 ? (
                <Button
                  onClick={nextStep}
                  color="wcr.7"
                  size="lg"
                  style={{
                    minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
                    borderRadius: '12px 6px 12px 6px',
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px'
                  }}
                >
                  Next Step
                </Button>
              ) : (
                <Button
                  type="submit"
                  color="green"
                  size="lg"
                  loading={isSubmitting}
                  disabled={!form.formState.isValid}
                  leftSection={!isSubmitting && <IconCheck />}
                  style={{
                    minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
                    borderRadius: '12px 6px 12px 6px',
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px'
                  }}
                >
                  {isSubmitting ? 'Submitting Application...' : 'Submit Application'}
                </Button>
              )}
            </Group>
          </Group>

          {/* Form errors */}
          {Object.keys(form.formState.errors).length > 0 && (
            <>
              <Divider my="md" />
              <Alert 
                icon={<IconAlertCircle />}
                color="red" 
                title="Please correct the following errors:"
              >
                <Stack gap={4}>
                  {Object.entries(form.formState.errors).map(([key, error]) => (
                    <Text key={key} size="sm">
                      {error?.message || `Error in ${key}`}
                    </Text>
                  ))}
                </Stack>
              </Alert>
            </>
          )}
        </Paper>
      </form>
    </Box>
  );
};