import React, { useState } from 'react';
import { Container, Title, Tabs, TextInput, Textarea, Button, Group, Stack, Box, Text } from '@mantine/core';
import { useForm } from '@mantine/form';
import type { UpdateProfileDto, ChangePasswordDto, UserProfileDto } from '../../types/dashboard.types';

/**
 * Profile Settings Page with 4 tabs
 * - Personal: Scene name, email, pronouns, bio
 * - Social: Discord, FetLife, phone
 * - Security: Change password
 * - Vetting: Read-only vetting status with membership hold option
 */
export const ProfileSettingsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<string | null>('personal');

  // Mock data - replace with actual API call
  const mockProfile: UserProfileDto = {
    userId: '1',
    sceneName: 'ShadowKnot',
    firstName: 'Shadow',
    lastName: 'Knot',
    email: 'shadowknot@example.com',
    pronouns: 'they/them',
    bio: 'Rope enthusiast since 2023',
    discordName: 'ShadowKnot#1234',
    fetLifeName: 'ShadowKnot',
    phoneNumber: '555-123-4567',
    vettingStatus: 'Pending',
  };

  return (
    <Box style={{ background: 'var(--color-cream)', minHeight: '100vh' }} pb="xl">
      <Container size="xl" py="xl">
        {/* Page Title */}
        <Title
          order={1}
          mb="xl"
          tt="uppercase"
          style={{
            fontFamily: 'var(--font-heading)',
            color: 'var(--color-burgundy)',
            fontSize: '2rem',
          }}
        >
          Profile Settings
        </Title>

        {/* Tabs */}
        <Tabs
          value={activeTab}
          onChange={setActiveTab}
          color="burgundy"
          styles={{
            tab: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              fontSize: '16px',
              padding: '12px 24px',
              '&[data-active]': {
                color: 'var(--color-burgundy)',
                borderColor: 'var(--color-burgundy)',
              },
            },
            panel: {
              paddingTop: '24px',
            },
          }}
        >
          <Tabs.List>
            <Tabs.Tab value="personal">Personal</Tabs.Tab>
            <Tabs.Tab value="social">Social</Tabs.Tab>
            <Tabs.Tab value="security">Security</Tabs.Tab>
            <Tabs.Tab value="vetting">Vetting</Tabs.Tab>
          </Tabs.List>

          <Tabs.Panel value="personal">
            <PersonalInfoForm profile={mockProfile} />
          </Tabs.Panel>

          <Tabs.Panel value="social">
            <SocialLinksForm profile={mockProfile} />
          </Tabs.Panel>

          <Tabs.Panel value="security">
            <ChangePasswordForm />
          </Tabs.Panel>

          <Tabs.Panel value="vetting">
            <VettingStatusDisplay profile={mockProfile} />
          </Tabs.Panel>
        </Tabs>
      </Container>
    </Box>
  );
};

// Personal Info Form Component
const PersonalInfoForm: React.FC<{ profile: UserProfileDto }> = ({ profile }) => {
  const form = useForm<UpdateProfileDto>({
    initialValues: {
      sceneName: profile.sceneName,
      firstName: profile.firstName || '',
      lastName: profile.lastName || '',
      email: profile.email,
      pronouns: profile.pronouns || '',
      bio: profile.bio || '',
      discordName: profile.discordName || '',
      fetLifeName: profile.fetLifeName || '',
      phoneNumber: profile.phoneNumber || '',
    },
  });

  const handleSubmit = (values: UpdateProfileDto) => {
    console.log('Update profile:', values);
    // TODO: Implement actual API call
  };

  return (
    <Box
      component="form"
      onSubmit={form.onSubmit(handleSubmit)}
      p="md"
      style={{
        background: 'white',
        borderRadius: '12px',
        border: '1px solid var(--color-taupe)',
      }}
    >
      <Stack gap="md">
        {/* Side-by-side inputs using Group with grow */}
        <Group grow align="flex-start">
          <TextInput
            label="Scene Name"
            placeholder="Your scene name"
            required
            {...form.getInputProps('sceneName')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
          <TextInput
            label="Email"
            placeholder="your@email.com"
            type="email"
            required
            {...form.getInputProps('email')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
        </Group>

        <Group grow align="flex-start">
          <TextInput
            label="First Name"
            placeholder="Optional"
            {...form.getInputProps('firstName')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
          <TextInput
            label="Last Name"
            placeholder="Optional"
            {...form.getInputProps('lastName')}
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                color: 'var(--color-burgundy)',
              },
            }}
          />
        </Group>

        <TextInput
          label="Pronouns"
          placeholder="e.g., they/them, she/her, he/him"
          {...form.getInputProps('pronouns')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <Textarea
          label="Bio"
          placeholder="Tell us about yourself..."
          rows={4}
          maxLength={500}
          {...form.getInputProps('bio')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <Group justify="flex-end">
          <Button
            type="submit"
            color="burgundy"
            style={{
              borderRadius: '12px 6px 12px 6px',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              textTransform: 'uppercase',
            }}
          >
            Save Changes
          </Button>
        </Group>
      </Stack>
    </Box>
  );
};

// Social Links Form Component
const SocialLinksForm: React.FC<{ profile: UserProfileDto }> = ({ profile }) => {
  const form = useForm<Pick<UpdateProfileDto, 'discordName' | 'fetLifeName' | 'phoneNumber'>>({
    initialValues: {
      discordName: profile.discordName || '',
      fetLifeName: profile.fetLifeName || '',
      phoneNumber: profile.phoneNumber || '',
    },
  });

  const handleSubmit = (values: Pick<UpdateProfileDto, 'discordName' | 'fetLifeName' | 'phoneNumber'>) => {
    console.log('Update social links:', values);
    // TODO: Implement actual API call
  };

  return (
    <Box
      component="form"
      onSubmit={form.onSubmit(handleSubmit)}
      p="md"
      style={{
        background: 'white',
        borderRadius: '12px',
        border: '1px solid var(--color-taupe)',
      }}
    >
      <Stack gap="md">
        <TextInput
          label="Discord Username"
          placeholder="Username#1234"
          {...form.getInputProps('discordName')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <TextInput
          label="FetLife Username"
          placeholder="Your FetLife username"
          {...form.getInputProps('fetLifeName')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <TextInput
          label="Phone Number"
          placeholder="555-123-4567"
          type="tel"
          {...form.getInputProps('phoneNumber')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <Group justify="flex-end">
          <Button
            type="submit"
            color="burgundy"
            style={{
              borderRadius: '12px 6px 12px 6px',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              textTransform: 'uppercase',
            }}
          >
            Save Changes
          </Button>
        </Group>
      </Stack>
    </Box>
  );
};

// Change Password Form Component
const ChangePasswordForm: React.FC = () => {
  const form = useForm<ChangePasswordDto>({
    initialValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
    validate: {
      newPassword: (value) => {
        if (value.length < 8) {
          return 'Password must be at least 8 characters';
        }
        if (!/(?=.*[a-z])/.test(value)) {
          return 'Password must contain lowercase letter';
        }
        if (!/(?=.*[A-Z])/.test(value)) {
          return 'Password must contain uppercase letter';
        }
        if (!/(?=.*\d)/.test(value)) {
          return 'Password must contain number';
        }
        return null;
      },
      confirmPassword: (value, values) =>
        value !== values.newPassword ? 'Passwords do not match' : null,
    },
  });

  const handleSubmit = (values: ChangePasswordDto) => {
    console.log('Change password');
    // TODO: Implement actual API call
    form.reset();
  };

  return (
    <Box
      component="form"
      onSubmit={form.onSubmit(handleSubmit)}
      p="md"
      style={{
        background: 'white',
        borderRadius: '12px',
        border: '1px solid var(--color-taupe)',
      }}
    >
      <Stack gap="md">
        <TextInput
          label="Current Password"
          type="password"
          required
          {...form.getInputProps('currentPassword')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <TextInput
          label="New Password"
          type="password"
          required
          {...form.getInputProps('newPassword')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <TextInput
          label="Confirm New Password"
          type="password"
          required
          {...form.getInputProps('confirmPassword')}
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
          }}
        />

        <Text size="sm" c="dimmed">
          Password must be at least 8 characters and contain uppercase, lowercase, and number.
        </Text>

        <Group justify="flex-end">
          <Button
            type="submit"
            color="burgundy"
            style={{
              borderRadius: '12px 6px 12px 6px',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              textTransform: 'uppercase',
            }}
          >
            Change Password
          </Button>
        </Group>
      </Stack>
    </Box>
  );
};

// Vetting Status Display Component
const VettingStatusDisplay: React.FC<{ profile: UserProfileDto }> = ({ profile }) => {
  return (
    <Box
      p="md"
      style={{
        background: 'white',
        borderRadius: '12px',
        border: '1px solid var(--color-taupe)',
      }}
    >
      <Stack gap="md">
        <TextInput
          label="Vetting Status"
          value={profile.vettingStatus}
          readOnly
          styles={{
            label: {
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
            },
            input: {
              backgroundColor: 'var(--color-cream)',
              color: 'var(--color-charcoal)',
            },
          }}
        />

        <Text size="sm" c="dimmed">
          Your vetting status is managed by administrators. If you have questions about your status, please contact
          us.
        </Text>

        <Group justify="flex-end">
          <Button
            variant="outline"
            color="burgundy"
            style={{
              borderRadius: '12px 6px 12px 6px',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              textTransform: 'uppercase',
            }}
            onClick={() => {
              console.log('Put membership on hold');
              // TODO: Implement membership hold functionality
            }}
          >
            Put Membership On Hold
          </Button>
        </Group>
      </Stack>
    </Box>
  );
};
