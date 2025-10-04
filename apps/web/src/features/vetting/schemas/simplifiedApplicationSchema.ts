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

  pronouns: z.string()
    .max(50, 'Pronouns must be less than 50 characters')
    .optional(),

  fetLifeHandle: z.string()
    .max(50, 'FetLife handle must be less than 50 characters')
    .optional(),

  otherNames: z.string()
    .max(500, 'Other names must be less than 500 characters')
    .optional(),

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
  pronouns: '',
  fetLifeHandle: '',
  otherNames: '',
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
  pronouns: {
    maxLength: 'Pronouns must be less than 50 characters',
    optional: 'How you\'d like to be referred to (e.g., she/her, they/them)',
  },
  fetLifeHandle: {
    maxLength: 'FetLife handle must be less than 50 characters',
    optional: 'Optional - helps us verify community connections',
  },
  otherNames: {
    maxLength: 'Other names must be less than 500 characters',
    optional: 'Any other names, nicknames, or social media handles you have used in a kinky context',
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