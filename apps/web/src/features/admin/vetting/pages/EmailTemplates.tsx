import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Container,
  Title,
  Text,
  Group,
  Button,
  Stack,
  Paper,
  Table,
  Badge,
  ActionIcon,
  Modal,
  TextInput,
  Textarea
} from '@mantine/core';
import {
  IconArrowLeft,
  IconEdit,
  IconPlus,
  IconEye,
  IconCopy
} from '@tabler/icons-react';

interface EmailTemplate {
  id: string;
  name: string;
  subject: string;
  type: 'application_received' | 'interview_scheduled' | 'approved' | 'denied' | 'on_hold';
  isActive: boolean;
  lastModified: string;
}

// Mock data for now - in production this would come from an API
const mockTemplates: EmailTemplate[] = [
  {
    id: '1',
    name: 'Application Received',
    subject: 'WitchCityRope - Application Received',
    type: 'application_received',
    isActive: true,
    lastModified: '2025-09-20'
  },
  {
    id: '2',
    name: 'Interview Scheduled',
    subject: 'WitchCityRope - Interview Scheduled',
    type: 'interview_scheduled',
    isActive: true,
    lastModified: '2025-09-18'
  },
  {
    id: '3',
    name: 'Application Approved',
    subject: 'Welcome to WitchCityRope!',
    type: 'approved',
    isActive: true,
    lastModified: '2025-09-15'
  },
  {
    id: '4',
    name: 'Application On Hold',
    subject: 'WitchCityRope - Additional Information Required',
    type: 'on_hold',
    isActive: true,
    lastModified: '2025-09-10'
  }
];

export const EmailTemplates: React.FC = () => {
  const navigate = useNavigate();
  const [templates] = useState<EmailTemplate[]>(mockTemplates);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [selectedTemplate, setSelectedTemplate] = useState<EmailTemplate | null>(null);

  const handleBackToVetting = () => {
    navigate('/admin/vetting');
  };

  const handleEditTemplate = (template: EmailTemplate) => {
    setSelectedTemplate(template);
    setEditModalOpen(true);
  };

  const handleCreateTemplate = () => {
    setSelectedTemplate(null);
    setEditModalOpen(true);
  };

  const getTypeBadgeColor = (type: EmailTemplate['type']) => {
    switch (type) {
      case 'application_received':
        return 'blue';
      case 'interview_scheduled':
        return 'yellow';
      case 'approved':
        return 'green';
      case 'denied':
        return 'red';
      case 'on_hold':
        return 'orange';
      default:
        return 'gray';
    }
  };

  const formatTypeName = (type: EmailTemplate['type']) => {
    return type.split('_').map(word =>
      word.charAt(0).toUpperCase() + word.slice(1)
    ).join(' ');
  };

  return (
    <Container size="xl" py="xl">
      {/* Header */}
      <Group justify="space-between" align="center" mb="xl">
        <div>
          <Group mb="sm">
            <Button
              variant="subtle"
              leftSection={<IconArrowLeft size={16} />}
              onClick={handleBackToVetting}
              size="sm"
            >
              Back to Vetting Applications
            </Button>
          </Group>
          <Title
            order={1}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '32px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Email Templates
          </Title>
          <Text size="lg" c="dimmed" mt="xs">
            Manage automated email templates for vetting process
          </Text>
        </div>

        <Button
          leftSection={<IconPlus size={16} />}
          onClick={handleCreateTemplate}
          styles={{
            root: {
              backgroundColor: '#880124',
              fontWeight: 600,
              height: '44px',
              paddingTop: '12px',
              paddingBottom: '12px',
              fontSize: '14px',
              lineHeight: '1.2'
            }
          }}
        >
          CREATE TEMPLATE
        </Button>
      </Group>

      {/* Templates Table */}
      <Paper shadow="sm" radius="md">
        <Table striped highlightOnHover>
          <Table.Thead style={{ backgroundColor: '#880124', color: 'white' }}>
            <Table.Tr>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  TEMPLATE NAME
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  TYPE
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  SUBJECT
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  STATUS
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  LAST MODIFIED
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  ACTIONS
                </Text>
              </Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {templates.map((template) => (
              <Table.Tr key={template.id}>
                <Table.Td>
                  <Text size="sm" fw={600}>
                    {template.name}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Badge color={getTypeBadgeColor(template.type)} variant="light">
                    {formatTypeName(template.type)}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">
                    {template.subject}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Badge color={template.isActive ? 'green' : 'gray'}>
                    {template.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">
                    {new Date(template.lastModified).toLocaleDateString()}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <ActionIcon
                      variant="light"
                      color="blue"
                      onClick={() => handleEditTemplate(template)}
                      title="Edit template"
                    >
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="light"
                      color="gray"
                      title="Preview template"
                    >
                      <IconEye size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="light"
                      color="green"
                      title="Duplicate template"
                    >
                      <IconCopy size={16} />
                    </ActionIcon>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>

        {templates.length === 0 && (
          <Stack align="center" py="xl">
            <Text c="dimmed" size="lg">
              No email templates found
            </Text>
            <Button
              leftSection={<IconPlus size={16} />}
              onClick={handleCreateTemplate}
              variant="light"
            >
              Create your first template
            </Button>
          </Stack>
        )}
      </Paper>

      {/* Edit/Create Template Modal */}
      <Modal
        opened={editModalOpen}
        onClose={() => setEditModalOpen(false)}
        title={selectedTemplate ? 'Edit Email Template' : 'Create Email Template'}
        size="lg"
        centered
      >
        <Stack gap="md">
          <TextInput
            label="Template Name"
            placeholder="Enter template name"
            defaultValue={selectedTemplate?.name || ''}
            required
          />

          <TextInput
            label="Subject Line"
            placeholder="Enter email subject"
            defaultValue={selectedTemplate?.subject || ''}
            required
          />

          <Textarea
            label="Email Content"
            placeholder="Enter email template content..."
            minRows={8}
            description="You can use variables like {{applicantName}}, {{applicationNumber}}, etc."
          />

          <Group justify="flex-end" mt="md">
            <Button
              variant="light"
              onClick={() => setEditModalOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={() => {
                // TODO: Implement save functionality
                setEditModalOpen(false);
              }}
              styles={{
                root: {
                  backgroundColor: '#880124'
                }
              }}
            >
              {selectedTemplate ? 'Update Template' : 'Create Template'}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Container>
  );
};