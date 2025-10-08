import React from 'react'
import { Container, Title, Paper, Text } from '@mantine/core'

/**
 * Simplified Event Session Matrix Demo
 * Basic page structure for testing
 */
export const EventSessionMatrixDemoSimple: React.FC = () => {
  return (
    <Container size="xl" py="xl">
      <Title order={1} ta="center" mb="xl" c="burgundy">
        Event Session Matrix Demo (Simple)
      </Title>

      <Paper shadow="sm" radius="md" p="xl">
        <Text size="lg" mb="md">
          This is a simplified version to test if the basic page loads correctly.
        </Text>

        <Text mb="md">If you can see this message, then:</Text>

        <ul>
          <li>React routing is working</li>
          <li>Mantine components are loading</li>
          <li>The page structure is functional</li>
        </ul>

        <Text mt="md" c="dimmed">
          Next step: Complete form implementation with Tiptap editor
        </Text>
      </Paper>
    </Container>
  )
}
