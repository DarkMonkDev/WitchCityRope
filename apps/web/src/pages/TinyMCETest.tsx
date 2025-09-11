import React, { useState } from 'react';
import { Container, Title, Card, Text, Alert } from '@mantine/core';
import { TinyMCERichTextEditor } from '../components/forms/TinyMCERichTextEditor';

/**
 * Test page to verify TinyMCE editor functionality
 * This is a temporary page to test the TinyMCE implementation
 */
export const TinyMCETestPage: React.FC = () => {
  const [content, setContent] = useState('<p>Start typing to test TinyMCE...</p>');

  return (
    <Container size="lg" py="xl">
      <Title order={1} c="burgundy" mb="xl" ta="center">
        TinyMCE Editor Test
      </Title>

      <Alert color="blue" mb="xl">
        This page tests the TinyMCE rich text editor implementation. The editor should show:
        <ul style={{ marginTop: '8px' }}>
          <li>A proper toolbar with formatting options</li>
          <li>Rich text editing capabilities</li>
          <li>No fallback to plain textarea</li>
        </ul>
      </Alert>

      <Card shadow="md" p="xl" mb="xl">
        <Title order={2} mb="md">
          TinyMCE Rich Text Editor
        </Title>
        
        <TinyMCERichTextEditor
          label="Test Editor"
          description="This should be a TinyMCE editor with toolbar, not a plain textarea"
          value={content}
          onChange={setContent}
          height={300}
          placeholder="Type something here to test the editor..."
        />
      </Card>

      <Card shadow="md" p="xl">
        <Title order={3} mb="md">
          Editor Output (HTML)
        </Title>
        <Text size="sm" c="dimmed" mb="xs">
          This shows the raw HTML content from the editor:
        </Text>
        <pre style={{ 
          background: '#f8f9fa', 
          padding: '12px', 
          borderRadius: '4px', 
          fontSize: '12px',
          overflow: 'auto',
          border: '1px solid #dee2e6'
        }}>
          {content}
        </pre>
      </Card>
    </Container>
  );
};

export default TinyMCETestPage;