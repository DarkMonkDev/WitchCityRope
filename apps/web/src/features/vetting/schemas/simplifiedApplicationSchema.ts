// Simplified Application Form Validation Schema
// Uses Zod for client-side validation matching the new simplified form

import { z } from 'zod';

/**
 * Validation schema for the simplified vetting application form
 * Based on the approved UI mockups with reduced complexity
 */
export const simplifiedApplicationSchema = z.object({
  realName: z.string()
    .min(2, 'Real name must be at least 2 characters')
    .max(100, 'Real name must be less than 100 characters')
    .trim(),

  sceneName: z.string()
    .min(2, 'Scene name must be at least 2 characters')
    .max(50, 'Scene name must be less than 50 characters')
    .trim(),

  fetLifeHandle: z.string()
    .max(50, 'FetLife handle must be less than 50 characters')
    .optional()
    .or(z.literal('')), // Allow empty string

  email: z.string()
    .email('Please enter a valid email address')
    .trim(),

  whyJoin: z.string()
    .min(20, 'Please provide at least 20 characters explaining why you would like to join')
    .max(2000, 'Response must be less than 2000 characters')
    .trim(),

  experienceWithRope: z.string()
    .min(50, 'Please provide at least 50 characters describing your experience')
    .max(2000, 'Experience description must be less than 2000 characters')
    .trim(),

  agreesToCommunityStandards: z.boolean()
    .refine(val => val === true, 'You must agree to the community standards'),
});

export type SimplifiedApplicationFormData = z.infer<typeof simplifiedApplicationSchema>;

/**
 * Default form values
 */
export const defaultFormValues: SimplifiedApplicationFormData = {
  realName: '',
  sceneName: '',
  fetLifeHandle: '',
  email: '', // Will be pre-filled from auth context
  whyJoin: '',
  experienceWithRope: '',
  agreesToCommunityStandards: false,
};

/**
 * Field validation helper functions
 */
export const fieldValidationMessages = {
  realName: {
    required: 'Your real name is required for our records',
    minLength: 'Real name must be at least 2 characters',
    maxLength: 'Real name must be less than 100 characters',
  },
  sceneName: {
    required: 'Your preferred scene name is required',
    minLength: 'Scene name must be at least 2 characters',
    maxLength: 'Scene name must be less than 50 characters',
  },
  fetLifeHandle: {
    maxLength: 'FetLife handle must be less than 50 characters',
    optional: 'Optional - helps us verify community connections',
  },
  email: {
    required: 'Email address is required',
    invalid: 'Please enter a valid email address',
    readonly: "We'll use this email to contact you about your application",
  },
  whyJoin: {
    required: 'Please explain why you would like to join Witch City Rope',
    minLength: 'Please provide at least 20 characters explaining why you would like to join',
    maxLength: 'Response must be less than 2000 characters',
    placeholder: 'Tell us why you would like to join Witch City Rope and what you hope to gain from being part of our community...',
  },
  experienceWithRope: {
    required: 'Please describe your experience with rope bondage',
    minLength: 'Please provide at least 50 characters describing your experience',
    maxLength: 'Experience description must be less than 2000 characters',
    placeholder: 'Tell us about your experience with rope bondage, BDSM, or kink communities...',
  },
  agreesToCommunityStandards: {
    required: 'You must agree to all community standards to submit your application',
  },
} as const;