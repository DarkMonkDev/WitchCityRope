// CmsEditButton component
// Responsive edit button: Desktop sticky, Mobile FAB
// Uses viewport width prop for responsive behavior (compatible with Playwright testing)

import React from 'react'
import { Button, ActionIcon } from '@mantine/core'
import { IconEdit } from '@tabler/icons-react'

interface CmsEditButtonProps {
  onClick: () => void
  viewportWidth?: number
}

export const CmsEditButton: React.FC<CmsEditButtonProps> = ({
  onClick,
  viewportWidth = typeof window !== 'undefined' ? window.innerWidth : 1024
}) => {
  const isMobile = viewportWidth < 768

  // CRITICAL: Use explicit conditional rendering instead of Mantine responsive props
  // Mantine's hiddenFrom/visibleFrom don't work reliably with Playwright viewport changes
  if (isMobile) {
    // Mobile FAB (Floating Action Button)
    return (
      <ActionIcon
        onClick={onClick}
        size={56}
        radius="xl"
        variant="gradient"
        gradient={{ from: 'orange', to: 'red', deg: 45 }}
        style={{
          position: 'fixed', // MUST be fixed for FAB
          bottom: 24,
          right: 24,
          zIndex: 999999,
          boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
        }}
        aria-label="Edit page content"
        data-testid="cms-edit-fab"
      >
        <IconEdit size={24} />
      </ActionIcon>
    )
  }

  // Desktop sticky button
  return (
    <Button
      onClick={onClick}
      leftSection={<IconEdit size={18} />}
      variant="outline"
      color="red"
      size="md"
      style={{
        position: 'sticky',
        top: 16,
        float: 'right',
        marginLeft: 16,
        zIndex: 10,
      }}
      data-testid="cms-edit-button"
    >
      Edit
    </Button>
  )
}
