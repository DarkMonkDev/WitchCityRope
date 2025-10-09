import { z } from 'zod';

// Common validation patterns
const emailPattern = z.string().email('Please enter a valid email address');
const passwordPattern = z
  .string()
  .min(8, 'Password must be at least 8 characters')
  .regex(
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
    'Password must contain uppercase, lowercase, number, and special character'
  );
const sceneNamePattern = z
  .string()
  .min(2, 'Scene name must be at least 2 characters')
  .max(50, 'Scene name must not exceed 50 characters')
  .regex(
    /^[a-zA-Z0-9_-]+$/,
    'Scene name can only contain letters, numbers, hyphens, and underscores'
  );
const phonePattern = z
  .string()
  .regex(/^\+?1?[0-9]{10,14}$/, 'Please enter a valid phone number');

// Authentication schemas
export const loginSchema = z.object({
  email: emailPattern,
  password: z.string().min(1, 'Password is required'),
  rememberMe: z.boolean().optional()
});

export const registerSchema = z
  .object({
    email: emailPattern,
    sceneName: sceneNamePattern,
    password: passwordPattern,
    confirmPassword: z.string(),
    firstName: z.string().optional(),
    lastName: z.string().optional(),
    bio: z.string().max(500, 'Bio must not exceed 500 characters').optional(),
    agreeToTerms: z.boolean().refine(val => val === true, {
      message: 'You must agree to the terms and conditions'
    }),
    agreeToNewsletter: z.boolean().optional()
  })
  .refine(data => data.password === data.confirmPassword, {
    message: "Passwords don't match",
    path: ['confirmPassword']
  });

export const forgotPasswordSchema = z.object({
  email: emailPattern
});

export const resetPasswordSchema = z
  .object({
    password: passwordPattern,
    confirmPassword: z.string(),
    token: z.string()
  })
  .refine(data => data.password === data.confirmPassword, {
    message: "Passwords don't match",
    path: ['confirmPassword']
  });

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required'),
    newPassword: passwordPattern,
    confirmPassword: z.string()
  })
  .refine(data => data.newPassword === data.confirmPassword, {
    message: "Passwords don't match",
    path: ['confirmPassword']
  });

// User profile schemas
export const userProfileSchema = z.object({
  sceneName: sceneNamePattern,
  firstName: z.string().max(50, 'First name must not exceed 50 characters').optional(),
  lastName: z.string().max(50, 'Last name must not exceed 50 characters').optional(),
  bio: z.string().max(1000, 'Bio must not exceed 1000 characters').optional(),
  location: z.string().max(100, 'Location must not exceed 100 characters').optional(),
  website: z
    .string()
    .url('Please enter a valid URL')
    .optional()
    .or(z.literal('')),
  experienceLevel: z.enum(['beginner', 'intermediate', 'advanced', 'expert']).optional(),
  interests: z.array(z.string()).optional()
});

// Event schemas
export const eventRegistrationSchema = z.object({
  eventId: z.string().uuid('Invalid event ID'),
  attendeeType: z.enum(['member', 'guest'], {
    message: 'Please select attendee type'
  }),
  medicalInfo: z.string().max(500, 'Medical info must not exceed 500 characters').optional(),
  dietaryRestrictions: z.string().max(500, 'Dietary restrictions must not exceed 500 characters').optional(),
  accessibilityNeeds: z.string().max(500, 'Accessibility needs must not exceed 500 characters').optional(),
  waiverSigned: z.boolean().refine(val => val === true, {
    message: 'You must sign the waiver to register'
  }),
  additionalNotes: z.string().max(1000, 'Additional notes must not exceed 1000 characters').optional()
});

export const createEventSchema = z.object({
  title: z.string().min(1, 'Event title is required').max(100, 'Title must not exceed 100 characters'),
  description: z.string().min(1, 'Event description is required'),
  startDate: z.string().datetime('Invalid start date'),
  endDate: z.string().datetime('Invalid end date'),
  location: z.string().min(1, 'Location is required').max(200, 'Location must not exceed 200 characters'),
  capacity: z.number().int().min(1, 'Capacity must be at least 1').max(500, 'Capacity cannot exceed 500'),
  price: z.number().min(0, 'Price cannot be negative').optional(),
  requiresVetting: z.boolean().optional(),
  ageRestriction: z.number().int().min(18).max(99).optional(),
  tags: z.array(z.string()).optional(),
  instructorId: z.string().uuid().optional(),
  materials: z.string().optional(),
  prerequisites: z.string().optional()
}).refine(data => new Date(data.endDate) > new Date(data.startDate), {
  message: 'End date must be after start date',
  path: ['endDate']
});

// Contact and communication schemas
export const contactFormSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must not exceed 100 characters'),
  email: emailPattern,
  subject: z.string().min(1, 'Subject is required').max(200, 'Subject must not exceed 200 characters'),
  message: z.string().min(10, 'Message must be at least 10 characters').max(2000, 'Message must not exceed 2000 characters'),
  type: z.enum(['general', 'support', 'membership', 'events', 'feedback'], {
    message: 'Please select a contact type'
  })
});

export const messageSchema = z.object({
  recipientId: z.string().uuid('Invalid recipient'),
  subject: z.string().min(1, 'Subject is required').max(200, 'Subject must not exceed 200 characters'),
  content: z.string().min(1, 'Message content is required').max(5000, 'Message must not exceed 5000 characters'),
  priority: z.enum(['low', 'normal', 'high']).optional()
});

// Admin schemas
export const userManagementSchema = z.object({
  userId: z.string().uuid('Invalid user ID'),
  sceneName: sceneNamePattern,
  email: emailPattern,
  isActive: z.boolean(),
  emailConfirmed: z.boolean(),
  roles: z.array(z.string()),
  membershipType: z.enum(['guest', 'member', 'vetted', 'instructor', 'admin']),
  notes: z.string().max(1000, 'Notes must not exceed 1000 characters').optional()
});

export const vettingApplicationSchema = z.object({
  applicantId: z.string().uuid('Invalid applicant ID'),
  experienceLevel: z.enum(['beginner', 'intermediate', 'advanced', 'expert']),
  ropeExperience: z.string().min(50, 'Please provide at least 50 characters about your rope experience'),
  safetyKnowledge: z.string().min(50, 'Please provide at least 50 characters about your safety knowledge'),
  communityInvolvement: z.string().optional(),
  references: z.array(z.object({
    name: z.string().min(1, 'Reference name is required'),
    email: emailPattern,
    relationship: z.string().min(1, 'Relationship is required'),
    yearsKnown: z.number().int().min(0).max(50)
  })).min(2, 'At least 2 references are required'),
  criminalBackground: z.boolean(),
  criminalBackgroundDetails: z.string().optional(),
  additionalInfo: z.string().max(2000, 'Additional info must not exceed 2000 characters').optional(),
  agreeToBackgroundCheck: z.boolean().refine(val => val === true, {
    message: 'You must agree to the background check'
  }),
  agreeToGuidelines: z.boolean().refine(val => val === true, {
    message: 'You must agree to follow community guidelines'
  })
});

// Payment and billing schemas
export const paymentMethodSchema = z.object({
  type: z.enum(['card', 'bank', 'paypal']),
  cardNumber: z.string().optional(),
  expiryMonth: z.number().int().min(1).max(12).optional(),
  expiryYear: z.number().int().min(new Date().getFullYear()).optional(),
  cvv: z.string().length(3).optional(),
  nameOnCard: z.string().optional(),
  billingAddress: z.object({
    line1: z.string(),
    line2: z.string().optional(),
    city: z.string(),
    state: z.string(),
    postalCode: z.string(),
    country: z.string()
  }).optional()
});

export const membershipPaymentSchema = z.object({
  membershipType: z.enum(['monthly', 'annual', 'lifetime']),
  paymentMethod: paymentMethodSchema,
  promoCode: z.string().optional(),
  agreeToTerms: z.boolean().refine(val => val === true, {
    message: 'You must agree to the membership terms'
  })
});

// Search and filter schemas
export const eventSearchSchema = z.object({
  query: z.string().optional(),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
  location: z.string().optional(),
  experienceLevel: z.enum(['beginner', 'intermediate', 'advanced', 'expert']).optional(),
  requiresVetting: z.boolean().optional(),
  tags: z.array(z.string()).optional(),
  priceMin: z.number().min(0).optional(),
  priceMax: z.number().min(0).optional(),
  instructorId: z.string().uuid().optional()
});

export const userSearchSchema = z.object({
  query: z.string().optional(),
  membershipType: z.enum(['guest', 'member', 'vetted', 'instructor', 'admin']).optional(),
  isActive: z.boolean().optional(),
  experienceLevel: z.enum(['beginner', 'intermediate', 'advanced', 'expert']).optional(),
  location: z.string().optional(),
  joinedAfter: z.string().datetime().optional(),
  joinedBefore: z.string().datetime().optional()
});

// Export type inference helpers
export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;
export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;
export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;
export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;
export type UserProfileFormData = z.infer<typeof userProfileSchema>;
export type EventRegistrationFormData = z.infer<typeof eventRegistrationSchema>;
export type CreateEventFormData = z.infer<typeof createEventSchema>;
export type ContactFormData = z.infer<typeof contactFormSchema>;
export type MessageFormData = z.infer<typeof messageSchema>;
export type UserManagementFormData = z.infer<typeof userManagementSchema>;
export type VettingApplicationFormData = z.infer<typeof vettingApplicationSchema>;
export type PaymentMethodFormData = z.infer<typeof paymentMethodSchema>;
export type MembershipPaymentFormData = z.infer<typeof membershipPaymentSchema>;
export type EventSearchFormData = z.infer<typeof eventSearchSchema>;
export type UserSearchFormData = z.infer<typeof userSearchSchema>;