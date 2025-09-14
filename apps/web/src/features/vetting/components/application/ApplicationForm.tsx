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
import type { ApplicationFormData } from '../../types/vetting.types';
import { TOUCH_TARGETS } from '../../types/vetting.types';

// Validation schema for complete form
const applicationSchema = z.object({
  personalInfo: z.object({
    fullName: z.string().min(2, 'Full name must be at least 2 characters'),
    sceneName: z.string().min(2, 'Scene name must be at least 2 characters'),
    pronouns: z.string().optional(),
    email: z.string().email('Please enter a valid email address'),
    phone: z.string().optional()
  }),
  experience: z.object({
    level: z.number().min(1).max(4),
    yearsExperience: z.number().min(0).max(50),
    description: z.string().min(50, 'Please provide at least 50 characters'),
    safetyKnowledge: z.string().min(30, 'Please describe your safety knowledge'),
    consentUnderstanding: z.string().min(30, 'Please describe your understanding of consent')
  }),
  community: z.object({
    whyJoin: z.string().min(50, 'Please tell us why you want to join (minimum 50 characters)'),
    skillsInterests: z.array(z.string()).min(1, 'Please select at least one skill or interest'),
    expectations: z.string().min(30, 'Please describe your expectations'),
    agreesToGuidelines: z.boolean().refine(val => val === true, 'You must agree to community guidelines')
  }),
  references: z.object({
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
    }).optional()
  }),
  review: z.object({
    agreesToTerms: z.boolean().refine(val => val === true, 'You must agree to terms and conditions'),
    isAnonymous: z.boolean(),
    consentToContact: z.boolean()
  })
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

  // Initialize form with validation
  const form = useForm<ApplicationFormData>({
    resolver: zodResolver(applicationSchema),
    defaultValues: {
      personalInfo: {
        fullName: '',
        sceneName: '',
        pronouns: '',
        email: '',
        phone: ''
      },
      experience: {
        level: 1,
        yearsExperience: 0,
        description: '',
        safetyKnowledge: '',
        consentUnderstanding: ''
      },
      community: {
        whyJoin: '',
        skillsInterests: [],
        expectations: '',
        agreesToGuidelines: false
      },
      references: {
        reference1: { name: '', email: '', relationship: '' },
        reference2: { name: '', email: '', relationship: '' },
        reference3: { name: '', email: '', relationship: '' }
      },
      review: {
        agreesToTerms: false,
        isAnonymous: false,
        consentToContact: true
      }
    },
    mode: 'onChange'
  });

  // Load draft data on component mount
  useEffect(() => {
    if (draftData && !isDraftLoading) {
      // Transform draft data to form format
      form.reset({
        personalInfo: {
          fullName: draftData.fullName || '',
          sceneName: draftData.sceneName || '',
          pronouns: draftData.pronouns || '',
          email: draftData.email || '',
          phone: draftData.phone || ''
        },
        experience: {
          level: draftData.experienceLevel || 1,
          yearsExperience: draftData.yearsExperience || 0,
          description: draftData.experienceDescription || '',
          safetyKnowledge: draftData.safetyKnowledge || '',
          consentUnderstanding: draftData.consentUnderstanding || ''
        },
        community: {
          whyJoin: draftData.whyJoinCommunity || '',
          skillsInterests: draftData.skillsInterests || [],
          expectations: draftData.expectationsGoals || '',
          agreesToGuidelines: draftData.agreesToGuidelines || false
        },
        references: {
          reference1: draftData.references?.[0] || { name: '', email: '', relationship: '' },
          reference2: draftData.references?.[1] || { name: '', email: '', relationship: '' },
          reference3: draftData.references?.[2] || { name: '', email: '', relationship: '' }
        },
        review: {
          agreesToTerms: draftData.agreesToTerms || false,
          isAnonymous: draftData.isAnonymous || false,
          consentToContact: draftData.consentToContact || true
        }
      });
    }
  }, [draftData, isDraftLoading, form]);

  // Auto-save functionality
  const handleAutoSave = useCallback(async () => {
    const formData = form.getValues();
    const email = formData.personalInfo.email;

    if (!email) return;

    setIsAutoSaving(true);
    try {
      await autoSaveDraft(formData, email);
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
    const formData = form.getValues();
    
    switch (currentStep) {
      case 0: // Personal Info
        return form.trigger(['personalInfo.fullName', 'personalInfo.sceneName', 'personalInfo.email']);
      case 1: // Experience
        return form.trigger([
          'experience.level',
          'experience.yearsExperience',
          'experience.description',
          'experience.safetyKnowledge',
          'experience.consentUnderstanding'
        ]);
      case 2: // Community
        return form.trigger([
          'community.whyJoin',
          'community.skillsInterests',
          'community.expectations',
          'community.agreesToGuidelines'
        ]);
      case 3: // References
        return form.trigger([
          'references.reference1.name',
          'references.reference1.email',
          'references.reference1.relationship',
          'references.reference2.name',
          'references.reference2.email',
          'references.reference2.relationship'
        ]);
      case 4: // Review
        return form.trigger(['review.agreesToTerms']);
      default:
        return true;
    }
  };

  // Handle form submission
  const handleSubmit = async (data: ApplicationFormData) => {
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
      onSubmissionComplete?.(
        submissionResult.applicationNumber,
        submissionResult.statusCheckUrl
      );
    }
  }, [submissionResult, clearDraft, onSubmissionComplete]);

  // Step components
  const stepComponents = [
    <PersonalInfoStep key="personal" form={form} />,
    <ExperienceStep key="experience" form={form} />,
    <CommunityStep key="community" form={form} />,
    <ReferencesStep key="references" form={form} />,
    <ReviewStep key="review" form={form} />
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
        <Stack spacing="md">
          <Title order={1} size="h2" ta="center" c="wcr.7">
            Join Our Community
          </Title>
          <Text ta="center" c="dimmed" size="lg">
            Complete our vetting process to become a member of WitchCityRope
          </Text>
          
          {/* Progress indicators */}
          <Group position="apart" align="center">
            <Text size="sm" c="dimmed">
              Step {currentStep + 1} of {stepLabels.length}
            </Text>
            {(isAutoSaving || lastSaved) && (
              <Group spacing="xs">
                {isAutoSaving ? (
                  <Text size="xs" c="blue">Saving draft...</Text>
                ) : lastSaved && (
                  <Group spacing={4}>
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

      <form onSubmit={form.handleSubmit(handleSubmit)}>
        {/* Stepper */}
        <Stepper
          active={currentStep}
          onStepClick={setCurrentStep}
          allowNextStepsSelect={false}
          breakpoint="sm"
          iconSize={40}
          styles={(theme) => ({
            stepIcon: {
              borderWidth: 2,
              '&[data-completed]': {
                backgroundColor: theme.colors.wcr[7],
                borderColor: theme.colors.wcr[7]
              }
            },
            stepLabel: {
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 600
            }
          })}
          mb="xl"
        >
          {stepLabels.map((label, index) => (
            <Stepper.Step key={index} label={label}>
              {/* Step content will be rendered below */}
            </Stepper.Step>
          ))}
        </Stepper>

        {/* Step Content */}
        <Paper p="xl" shadow="sm" mb="xl">
          {stepComponents[currentStep]}
        </Paper>

        {/* Navigation */}
        <Paper p="lg" shadow="sm">
          <Group position="apart">
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
                  leftIcon={!isSubmitting && <IconCheck />}
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
                <Stack spacing={4}>
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