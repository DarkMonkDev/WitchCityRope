import React, { useState } from 'react';
import {
  Container,
  Title,
  Paper,
  Stack,
  Group,
  Text,
  Button,
  Grid,
  Badge,
  Accordion,
  Code,
  Alert
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconInfoCircle, IconCheck, IconX } from '@tabler/icons-react';
import {
  BaseInput,
  BaseSelect,
  BaseTextarea,
  EmailInput,
  PasswordInput,
  SceneNameInput,
  PhoneInput,
  EmergencyContactGroup
} from '../components/forms';
import { mockCheckEmailUnique, mockCheckSceneNameUnique, mockSubmitForm } from '../utils/mockApi';

interface FormData {
  basicInput: string;
  email: string;
  sceneName: string;
  password: string;
  phone: string;
  textarea: string;
  select: string;
  emergencyContact: {
    name: string;
    phone: string;
    relationship: string;
  };
}

export const FormComponentsTest: React.FC = () => {
  const [formStates, setFormStates] = useState({
    showErrors: false,
    isSubmitting: false,
    disableAll: false
  });

  const form = useForm<FormData>({
    initialValues: {
      basicInput: '',
      email: '',
      sceneName: '',
      password: '',
      phone: '',
      textarea: '',
      select: '',
      emergencyContact: {
        name: '',
        phone: '',
        relationship: ''
      }
    },
    validate: {
      basicInput: (value) => value.length < 2 ? 'Basic input must be at least 2 characters' : null,
      email: (value) => {
        if (!value) return 'Email is required';
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return !emailRegex.test(value) ? 'Please enter a valid email address' : null;
      },
      sceneName: (value) => {
        if (!value) return 'Scene name is required';
        if (value.length < 2) return 'Scene name must be at least 2 characters';
        if (value.length > 50) return 'Scene name must not exceed 50 characters';
        const sceneNameRegex = /^[a-zA-Z0-9_-]+$/;
        return !sceneNameRegex.test(value) ? 'Scene name can only contain letters, numbers, hyphens, and underscores' : null;
      },
      password: (value) => {
        if (!value) return 'Password is required';
        if (value.length < 8) return 'Password must be at least 8 characters';
        const hasUpper = /[A-Z]/.test(value);
        const hasLower = /[a-z]/.test(value);
        const hasNumber = /\d/.test(value);
        const hasSpecial = /[@$!%*?&]/.test(value);
        if (!hasUpper) return 'Password must contain uppercase letter';
        if (!hasLower) return 'Password must contain lowercase letter';
        if (!hasNumber) return 'Password must contain number';
        if (!hasSpecial) return 'Password must contain special character';
        return null;
      },
      select: (value) => !value ? 'Please select an option' : null,
      textarea: (value) => value.length > 500 ? 'Textarea must not exceed 500 characters' : null
    }
  });

  const handleSubmit = async (values: FormData) => {
    setFormStates(prev => ({ ...prev, isSubmitting: true }));
    
    try {
      await mockSubmitForm(values);
      notifications.show({
        title: 'Success!',
        message: 'Form submitted successfully',
        color: 'green',
        icon: <IconCheck size={16} />
      });
      form.reset();
    } catch (error) {
      notifications.show({
        title: 'Error',
        message: 'Form submission failed',
        color: 'red',
        icon: <IconX size={16} />
      });
    } finally {
      setFormStates(prev => ({ ...prev, isSubmitting: false }));
    }
  };

  const toggleErrors = () => {
    setFormStates(prev => ({ ...prev, showErrors: !prev.showErrors }));
    if (!formStates.showErrors) {
      form.validate();
    } else {
      form.clearErrors();
    }
  };

  const toggleDisabled = () => {
    setFormStates(prev => ({ ...prev, disableAll: !prev.disableAll }));
  };

  const fillTestData = () => {
    form.setValues({
      basicInput: 'Test Input Value',
      email: 'test@example.com',
      sceneName: 'TestUser123',
      password: 'SecurePass123!',
      phone: '(555) 123-4567',
      textarea: 'This is a test textarea with some sample content.',
      select: 'option2',
      emergencyContact: {
        name: 'Jane Doe',
        phone: '(555) 987-6543',
        relationship: 'Friend'
      }
    });
  };

  const fillConflictData = () => {
    form.setValues({
      basicInput: 'Conflict Test',
      email: 'taken@example.com', // This will trigger uniqueness error
      sceneName: 'admin', // This will trigger uniqueness error
      password: 'weak', // This will trigger strength errors
      phone: '555123',
      textarea: '',
      select: '',
      emergencyContact: {
        name: '',
        phone: '',
        relationship: ''
      }
    });
  };

  const selectOptions = [
    { value: 'option1', label: 'Option 1' },
    { value: 'option2', label: 'Option 2' },
    { value: 'option3', label: 'Option 3' }
  ];

  const relationshipOptions = [
    { value: 'spouse', label: 'Spouse/Partner' },
    { value: 'parent', label: 'Parent' },
    { value: 'sibling', label: 'Sibling' },
    { value: 'friend', label: 'Friend' },
    { value: 'other', label: 'Other' }
  ];

  return (
    <Container size="lg" py="xl">
      <Stack gap="xl">
        {/* Header */}
        <div>
          <Title order={1} mb="md">Mantine v7 Form Components Test</Title>
          <Text c="dimmed" size="lg">
            Comprehensive testing page for all WitchCityRope form components with various states and interactions.
          </Text>
        </div>

        {/* Test Controls */}
        <Paper p="md" withBorder>
          <Title order={2} size="h3" mb="md">Test Controls</Title>
          <Group>
            <Button 
              onClick={fillTestData} 
              variant="light"
              data-testid="fill-test-data"
            >
              Fill Test Data
            </Button>
            <Button 
              onClick={fillConflictData} 
              variant="light" 
              color="orange"
              data-testid="fill-conflict-data"
            >
              Fill Conflict Data
            </Button>
            <Button 
              onClick={toggleErrors} 
              variant="light" 
              color={formStates.showErrors ? 'red' : 'blue'}
              data-testid="toggle-errors"
            >
              {formStates.showErrors ? 'Hide' : 'Show'} Validation Errors
            </Button>
            <Button 
              onClick={toggleDisabled} 
              variant="light" 
              color={formStates.disableAll ? 'red' : 'gray'}
              data-testid="toggle-disabled"
            >
              {formStates.disableAll ? 'Enable' : 'Disable'} All Fields
            </Button>
          </Group>
        </Paper>

        {/* Instructions */}
        <Alert icon={<IconInfoCircle size={16} />} title="Testing Instructions" color="blue">
          <Stack gap="xs">
            <Text size="sm">• Use "Fill Test Data" to populate all fields with valid data</Text>
            <Text size="sm">• Use "Fill Conflict Data" to test validation errors and async uniqueness checks</Text>
            <Text size="sm">• Try typing "taken@example.com" in email field to see async validation</Text>
            <Text size="sm">• Try typing "admin" in scene name field to see async validation</Text>
            <Text size="sm">• Password field shows real-time strength meter and requirements</Text>
            <Text size="sm">• Phone field auto-formats as you type (US format)</Text>
          </Stack>
        </Alert>

        {/* Main Form */}
        <form onSubmit={form.onSubmit(handleSubmit)}>
          <Stack gap="xl">
            
            {/* Basic Components */}
            <Paper p="md" withBorder>
              <Title order={2} size="h3" mb="md">Basic Form Components</Title>
              <Grid>
                <Grid.Col span={{ base: 12, md: 6 }}>
                  <BaseInput
                    label="Basic Input"
                    placeholder="Enter some text"
                    description="This is a basic text input with validation"
                    withAsterisk
                    disabled={formStates.disableAll}
                    data-testid="basic-input"
                    {...form.getInputProps('basicInput')}
                  />
                </Grid.Col>
                <Grid.Col span={{ base: 12, md: 6 }}>
                  <BaseSelect
                    label="Basic Select"
                    placeholder="Choose an option"
                    description="This is a basic select dropdown"
                    data={selectOptions}
                    withAsterisk
                    disabled={formStates.disableAll}
                    data-testid="basic-select"
                    {...form.getInputProps('select')}
                  />
                </Grid.Col>
                <Grid.Col span={12}>
                  <BaseTextarea
                    label="Basic Textarea"
                    placeholder="Enter your message here..."
                    description="This is a basic textarea (max 500 characters)"
                    rows={4}
                    maxLength={500}
                    disabled={formStates.disableAll}
                    data-testid="basic-textarea"
                    {...form.getInputProps('textarea')}
                  />
                </Grid.Col>
              </Grid>
            </Paper>

            {/* Specialized Components */}
            <Paper p="md" withBorder>
              <Title order={2} size="h3" mb="md">Specialized WitchCityRope Components</Title>
              <Grid>
                <Grid.Col span={{ base: 12, md: 6 }}>
                  <EmailInput
                    withAsterisk
                    description="Checks email uniqueness (try 'taken@example.com')"
                    checkUniqueness
                    onUniquenessCheck={mockCheckEmailUnique}
                    disabled={formStates.disableAll}
                    data-testid="email-input"
                    {...form.getInputProps('email')}
                  />
                </Grid.Col>
                <Grid.Col span={{ base: 12, md: 6 }}>
                  <SceneNameInput
                    label="Scene Name"
                    description="Checks scene name uniqueness (try 'admin')"
                    withAsterisk
                    checkUniqueness
                    onUniquenessCheck={mockCheckSceneNameUnique}
                    disabled={formStates.disableAll}
                    data-testid="scene-name-input"
                    {...form.getInputProps('sceneName')}
                  />
                </Grid.Col>
                <Grid.Col span={{ base: 12, md: 6 }}>
                  <PasswordInput
                    label="Password"
                    description="Shows real-time strength meter and requirements"
                    withAsterisk
                    showStrengthMeter
                    disabled={formStates.disableAll}
                    data-testid="password-input"
                    {...form.getInputProps('password')}
                  />
                </Grid.Col>
                <Grid.Col span={{ base: 12, md: 6 }}>
                  <PhoneInput
                    label="Phone Number"
                    description="Auto-formats US phone numbers as you type"
                    disabled={formStates.disableAll}
                    data-testid="phone-input"
                    {...form.getInputProps('phone')}
                  />
                </Grid.Col>
              </Grid>
            </Paper>

            {/* Emergency Contact Group */}
            <Paper p="md" withBorder>
              <Title order={2} size="h3" mb="md">Emergency Contact Group</Title>
              <EmergencyContactGroup
                form={form}
                basePath="emergencyContact"
                relationshipOptions={relationshipOptions}
                disabled={formStates.disableAll}
                data-testid="emergency-contact"
              />
            </Paper>

            {/* Form State Display */}
            <Paper p="md" withBorder>
              <Title order={2} size="h3" mb="md">Form State</Title>
              <Grid>
                <Grid.Col span={{ base: 12, md: 6 }}>
                  <Stack gap="xs">
                    <Group>
                      <Badge color={form.isValid() ? 'green' : 'red'}>
                        {form.isValid() ? 'Valid' : 'Invalid'}
                      </Badge>
                      <Badge color={form.isDirty() ? 'blue' : 'gray'}>
                        {form.isDirty() ? 'Dirty' : 'Pristine'}
                      </Badge>
                      <Badge color={formStates.isSubmitting ? 'yellow' : 'gray'}>
                        {formStates.isSubmitting ? 'Submitting' : 'Ready'}
                      </Badge>
                    </Group>
                  </Stack>
                </Grid.Col>
                <Grid.Col span={{ base: 12, md: 6 }}>
                  <Accordion variant="contained">
                    <Accordion.Item value="form-values">
                      <Accordion.Control>View Form Values</Accordion.Control>
                      <Accordion.Panel>
                        <Code block>
                          {JSON.stringify(form.values, null, 2)}
                        </Code>
                      </Accordion.Panel>
                    </Accordion.Item>
                    <Accordion.Item value="form-errors">
                      <Accordion.Control>View Form Errors</Accordion.Control>
                      <Accordion.Panel>
                        <Code block>
                          {JSON.stringify(form.errors, null, 2)}
                        </Code>
                      </Accordion.Panel>
                    </Accordion.Item>
                  </Accordion>
                </Grid.Col>
              </Grid>
            </Paper>

            {/* Submit Button */}
            <Group justify="center">
              <Button
                type="submit"
                size="lg"
                loading={formStates.isSubmitting}
                disabled={formStates.disableAll}
                data-testid="submit-button"
              >
                {formStates.isSubmitting ? 'Submitting...' : 'Submit Form'}
              </Button>
            </Group>

          </Stack>
        </form>

        {/* Component Documentation */}
        <Paper p="md" withBorder>
          <Title order={2} size="h3" mb="md">Component Features Demonstrated</Title>
          <Grid>
            <Grid.Col span={{ base: 12, md: 6 }}>
              <Stack gap="xs">
                <Text fw={700}>Interaction States:</Text>
                <Text size="sm">• Default/empty state</Text>
                <Text size="sm">• Hover effects (CSS)</Text>
                <Text size="sm">• Focus states</Text>
                <Text size="sm">• Filled states</Text>
                <Text size="sm">• Loading states (async validation)</Text>
                <Text size="sm">• Disabled states</Text>
              </Stack>
            </Grid.Col>
            <Grid.Col span={{ base: 12, md: 6 }}>
              <Stack gap="xs">
                <Text fw={700}>Validation Features:</Text>
                <Text size="sm">• Required field validation</Text>
                <Text size="sm">• Format validation (email, phone)</Text>
                <Text size="sm">• Async uniqueness validation</Text>
                <Text size="sm">• Password strength meter</Text>
                <Text size="sm">• Real-time feedback</Text>
                <Text size="sm">• Error message display</Text>
              </Stack>
            </Grid.Col>
          </Grid>
        </Paper>

      </Stack>
    </Container>
  );
};

export default FormComponentsTest;