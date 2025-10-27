// Simplified Application Form Validation Schema
// Uses Zod for client-side validation matching the new simplified form

import { z } from 'zod';

/**
 * Validation schema for the simplified vetting application form
 * Based on the approved UI mockups with reduced complexity
 */
export const simplifiedApplicationSchema = z.object({
  firstName: z.string()
    .min(1, 'First name is required')
    .max(50, 'First name must be less than 50 characters')
    .trim(),

  lastName: z.string()
    .min(1, 'Last name is required')
    .max(50, 'Last name must be less than 50 characters')
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
    .max(2000, 'Response must be less than 2000 characters')
    .trim()
    .refine(val => val.length > 0, 'This field is required'),

  experienceWithRope: z.string()
    .max(2000, 'Experience description must be less than 2000 characters')
    .trim()
    .refine(val => val.length > 0, 'This field is required'),

  agreeToCommunityStandards: z.boolean()
    .refine(val => val === true, 'You must agree to the community standards'),
});

export type SimplifiedApplicationFormData = z.infer<typeof simplifiedApplicationSchema>;

/**
 * Default form values
 */
export const defaultFormValues: SimplifiedApplicationFormData = {
  firstName: '',
  lastName: '',
  pronouns: '',
  fetLifeHandle: '',
  otherNames: '',
  whyJoin: '',
  experienceWithRope: '',
  agreeToCommunityStandards: false,
};

/**
 * Field validation helper functions
 */
export const fieldValidationMessages = {
  firstName: {
    required: 'First name is required',
    minLength: 'First name must be at least 1 character',
    maxLength: 'First name must be less than 50 characters',
  },
  lastName: {
    required: 'Last name is required',
    minLength: 'Last name must be at least 1 character',
    maxLength: 'Last name must be less than 50 characters',
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
    maxLength: 'Response must be less than 2000 characters',
    placeholder: 'Tell us why you would like to join Witch City Rope and what you hope to gain from being part of our community...',
  },
  experienceWithRope: {
    required: 'Please describe your experience with rope bondage',
    maxLength: 'Experience description must be less than 2000 characters',
    placeholder: 'Tell us about your experience with rope bondage, BDSM, or kink communities...',
  },
  agreeToCommunityStandards: {
    required: 'You must agree to all community standards to submit your application',
  },
} as const;