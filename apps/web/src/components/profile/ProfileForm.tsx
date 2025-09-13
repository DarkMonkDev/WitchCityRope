import React from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Box,
  TextInput,
  Textarea,
  Select,
  Switch,
  Button,
  Group,
  Stack,
  Tabs,
  Alert,
  FileInput,
  Text,
  Progress,
  Divider,
} from '@mantine/core';
import type { UserDto } from '@witchcityrope/shared-types';

// Validation schemas for different tabs
const personalInfoSchema = z.object({
  sceneName: z.string()
    .min(2, 'Scene name must be at least 2 characters')
    .max(50, 'Scene name cannot exceed 50 characters'),
  firstName: z.string().min(1, 'First name is required').max(50),
  lastName: z.string().min(1, 'Last name is required').max(50),
  pronouns: z.string().optional(),
  bio: z.string().max(500, 'Bio must be less than 500 characters').optional(),
  phoneNumber: z.string().optional(),
});

const privacySchema = z.object({
  profileVisibility: z.enum(['public', 'members', 'vetted', 'private']),
  showEmail: z.boolean(),
  showPhone: z.boolean(),
  showEventHistory: z.boolean(),
  allowMessages: z.boolean(),
});

const preferencesSchema = z.object({
  emailNotifications: z.boolean(),
  eventReminders: z.boolean(),
  communityUpdates: z.boolean(),
  marketingEmails: z.boolean(),
  notificationFrequency: z.enum(['immediate', 'daily', 'weekly']),
});

type PersonalInfoData = z.infer<typeof personalInfoSchema>;
type PrivacyData = z.infer<typeof privacySchema>;
type PreferencesData = z.infer<typeof preferencesSchema>;

interface ProfileFormProps {
  user?: UserDto | null;
  onSubmit: (data: any) => void;
  isLoading?: boolean;
  error?: string | null;
}

/**
 * Comprehensive Profile Management Form
 * Multi-tab interface for personal info, privacy settings, and preferences
 * Follows WitchCityRope Design System v7 patterns
 */
export const ProfileForm: React.FC<ProfileFormProps> = ({
  user,
  onSubmit,
  isLoading = false,
  error = null
}) => {
  const [activeTab, setActiveTab] = React.useState<string>('personal');
  const [profilePhoto, setProfilePhoto] = React.useState<File | null>(null);

  // Personal Info Form
  const personalForm = useForm<PersonalInfoData>({
    resolver: zodResolver(personalInfoSchema),
    defaultValues: {
      sceneName: user?.sceneName || '',
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      pronouns: '', // Add when available in UserDto
      bio: '', // Add when available in UserDto
      phoneNumber: user?.phoneNumber || '',
    }
  });

  // Privacy Settings Form
  const privacyForm = useForm<PrivacyData>({
    resolver: zodResolver(privacySchema),
    defaultValues: {
      profileVisibility: 'members',
      showEmail: false,
      showPhone: false,
      showEventHistory: true,
      allowMessages: true,
    }
  });

  // Preferences Form
  const preferencesForm = useForm<PreferencesData>({
    resolver: zodResolver(preferencesSchema),
    defaultValues: {
      emailNotifications: true,
      eventReminders: true,
      communityUpdates: true,
      marketingEmails: false,
      notificationFrequency: 'daily',
    }
  });

  // Calculate profile completion
  const calculateCompletion = () => {
    const personalData = personalForm.watch();
    let completed = 0;
    const totalFields = 6;

    if (personalData.sceneName) completed++;
    if (personalData.firstName) completed++;
    if (personalData.lastName) completed++;
    if (personalData.pronouns) completed++;
    if (personalData.bio) completed++;
    if (personalData.phoneNumber) completed++;

    return Math.round((completed / totalFields) * 100);
  };

  const profileCompletion = calculateCompletion();

  const handlePersonalInfoSubmit = (data: PersonalInfoData) => {
    onSubmit({
      type: 'personal',
      data: {
        ...data,
        profilePhoto,
      }
    });
  };

  const handlePrivacySubmit = (data: PrivacyData) => {
    onSubmit({
      type: 'privacy',
      data
    });
  };

  const handlePreferencesSubmit = (data: PreferencesData) => {
    onSubmit({
      type: 'preferences',
      data
    });
  };

  return (
    <Box>
      {error && (
        <Alert 
          color="red" 
          mb="md"
          style={{ 
            background: 'rgba(220, 20, 60, 0.05)', 
            border: '1px solid rgba(220, 20, 60, 0.2)' 
          }}
        >
          {error}
        </Alert>
      )}

      {/* Profile Completion Progress */}
      <Box
        mb="xl"
        p="md"
        style={{
          background: 'linear-gradient(135deg, #f8f4e6, #e8ddd4)',
          borderRadius: '8px',
          border: '1px solid rgba(183, 109, 117, 0.2)',
        }}
      >
        <Group justify="space-between" mb="xs">
          <Text
            fw={700}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              color: '#2B2B2B',
            }}
          >
            Profile Completion
          </Text>
          <Text
            fw={700}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              color: profileCompletion >= 80 ? '#228B22' : '#DAA520',
            }}
          >
            {profileCompletion}%
          </Text>
        </Group>
        <Progress
          value={profileCompletion}
          size="md"
          radius="md"
          style={{
            '& .mantine-Progress-bar': {
              background: profileCompletion >= 80 
                ? 'linear-gradient(90deg, #228B22, #32CD32)'
                : 'linear-gradient(90deg, #DAA520, #FFD700)',
            },
          }}
        />
        {profileCompletion < 80 && (
          <Text size="sm" c="dimmed" mt="xs">
            Complete your profile to unlock all community features
          </Text>
        )}
      </Box>

      <Tabs value={activeTab} onChange={setActiveTab}>
        <Tabs.List>
          <Tabs.Tab
            value="personal"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            Personal Info
          </Tabs.Tab>
          <Tabs.Tab
            value="privacy"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            Privacy
          </Tabs.Tab>
          <Tabs.Tab
            value="preferences"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            Preferences
          </Tabs.Tab>
        </Tabs.List>

        {/* Personal Information Tab */}
        <Tabs.Panel value="personal" pt="md">
          <form onSubmit={personalForm.handleSubmit(handlePersonalInfoSubmit)}>
            <Stack gap="md">
              <Group grow>
                <TextInput
                  label="Scene Name"
                  placeholder="Your community name"
                  required
                  {...personalForm.register('sceneName')}
                  error={personalForm.formState.errors.sceneName?.message}
                />
              </Group>

              <Group grow>
                <TextInput
                  label="First Name"
                  placeholder="Legal first name"
                  required
                  {...personalForm.register('firstName')}
                  error={personalForm.formState.errors.firstName?.message}
                />
                <TextInput
                  label="Last Name"
                  placeholder="Legal last name"
                  required
                  {...personalForm.register('lastName')}
                  error={personalForm.formState.errors.lastName?.message}
                />
              </Group>

              <Group grow>
                <TextInput
                  label="Email"
                  value={user?.email || ''}
                  disabled
                  description="Email cannot be changed from this page"
                />
                <Controller
                  name="pronouns"
                  control={personalForm.control}
                  render={({ field }) => (
                    <Select
                      label="Pronouns"
                      placeholder="Select pronouns"
                      data={[
                        { value: 'she/her', label: 'She/Her' },
                        { value: 'he/him', label: 'He/Him' },
                        { value: 'they/them', label: 'They/Them' },
                        { value: 'other', label: 'Other' },
                      ]}
                      value={field.value}
                      onChange={field.onChange}
                    />
                  )}
                />
              </Group>

              <TextInput
                label="Phone Number"
                placeholder="+1 (555) 123-4567"
                {...personalForm.register('phoneNumber')}
                error={personalForm.formState.errors.phoneNumber?.message}
              />

              <Textarea
                label="Bio / Introduction"
                placeholder="Tell the community about yourself, your interests, and experience level..."
                minRows={4}
                maxRows={6}
                {...personalForm.register('bio')}
                error={personalForm.formState.errors.bio?.message}
                description={`${personalForm.watch('bio')?.length || 0}/500 characters`}
              />

              <FileInput
                label="Profile Photo"
                placeholder="Upload a profile photo"
                accept="image/*"
                onChange={setProfilePhoto}
                description="JPG, PNG, or GIF. Max size: 5MB"
              />

              <Divider />

              <Group justify="flex-end">
                <Button
                  data-testid="button-save-profile"
                  type="submit"
                  color="wcr"
                  loading={isLoading}
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                  }}
                >
                  Save Personal Info
                </Button>
              </Group>
            </Stack>
          </form>
        </Tabs.Panel>

        {/* Privacy Settings Tab */}
        <Tabs.Panel value="privacy" pt="md">
          <form onSubmit={privacyForm.handleSubmit(handlePrivacySubmit)}>
            <Stack gap="md">
              <Controller
                name="profileVisibility"
                control={privacyForm.control}
                render={({ field }) => (
                  <Select
                    label="Profile Visibility"
                    description="Who can see your full profile information"
                    data={[
                      { value: 'public', label: 'Public - Anyone can see' },
                      { value: 'members', label: 'Members - Only registered members' },
                      { value: 'vetted', label: 'Vetted Only - Only vetted members' },
                      { value: 'private', label: 'Private - Only you and admins' },
                    ]}
                    value={field.value}
                    onChange={field.onChange}
                  />
                )}
              />

              <Divider label="Contact Information Visibility" />

              <Switch
                label="Show Email Address"
                description="Allow other members to see your email address"
                {...privacyForm.register('showEmail')}
              />

              <Switch
                label="Show Phone Number"
                description="Allow other members to see your phone number"
                {...privacyForm.register('showPhone')}
              />

              <Divider label="Community Features" />

              <Switch
                label="Show Event History"
                description="Allow others to see which events you've attended"
                {...privacyForm.register('showEventHistory')}
              />

              <Switch
                label="Allow Messages"
                description="Allow other members to send you messages"
                {...privacyForm.register('allowMessages')}
              />

              <Group justify="flex-end">
                <Button
                  type="submit"
                  color="wcr"
                  loading={isLoading}
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                  }}
                >
                  Save Privacy Settings
                </Button>
              </Group>
            </Stack>
          </form>
        </Tabs.Panel>

        {/* Preferences Tab */}
        <Tabs.Panel value="preferences" pt="md">
          <form onSubmit={preferencesForm.handleSubmit(handlePreferencesSubmit)}>
            <Stack gap="md">
              <Divider label="Email Notifications" />

              <Switch
                label="Event Reminders"
                description="Receive reminders about upcoming events you're registered for"
                {...preferencesForm.register('eventReminders')}
              />

              <Switch
                label="Community Updates"
                description="Receive newsletters and important community announcements"
                {...preferencesForm.register('communityUpdates')}
              />

              <Switch
                label="Email Notifications"
                description="General email notifications for account activity"
                {...preferencesForm.register('emailNotifications')}
              />

              <Switch
                label="Marketing Emails"
                description="Receive promotional emails about events and special offers"
                {...preferencesForm.register('marketingEmails')}
              />

              <Controller
                name="notificationFrequency"
                control={preferencesForm.control}
                render={({ field }) => (
                  <Select
                    label="Notification Frequency"
                    description="How often would you like to receive non-urgent notifications?"
                    data={[
                      { value: 'immediate', label: 'Immediate' },
                      { value: 'daily', label: 'Daily Digest' },
                      { value: 'weekly', label: 'Weekly Summary' },
                    ]}
                    value={field.value}
                    onChange={field.onChange}
                  />
                )}
              />

              <Group justify="flex-end">
                <Button
                  type="submit"
                  color="wcr"
                  loading={isLoading}
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                  }}
                >
                  Save Preferences
                </Button>
              </Group>
            </Stack>
          </form>
        </Tabs.Panel>
      </Tabs>
    </Box>
  );
};