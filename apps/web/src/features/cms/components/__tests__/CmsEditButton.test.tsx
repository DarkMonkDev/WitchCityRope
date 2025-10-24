import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MantineProvider } from '@mantine/core'
import { CmsEditButton } from '../CmsEditButton'

// Mock useMediaQuery to test responsive behavior
vi.mock('@mantine/hooks', () => ({
  useMediaQuery: vi.fn(),
}))

import { useMediaQuery } from '@mantine/hooks'

const renderWithProvider = (ui: React.ReactElement) => {
  return render(
    <MantineProvider>
      {ui}
    </MantineProvider>
  );
};

describe('CmsEditButton Component', () => {
  it('renders sticky button on desktop (>768px)', () => {
    // Mock desktop viewport
    vi.mocked(useMediaQuery).mockReturnValue(false)

    const onClick = vi.fn()
    renderWithProvider(<CmsEditButton onClick={onClick} />)

    const button = screen.getByRole('button', { name: /edit/i })
    expect(button).toBeInTheDocument()
    expect(button).toHaveTextContent(/edit/i) // Desktop shows text
  })

  it('renders FAB on mobile (<768px)', () => {
    // Mock mobile viewport
    vi.mocked(useMediaQuery).mockReturnValue(true)

    const onClick = vi.fn()
    renderWithProvider(<CmsEditButton onClick={onClick} />)

    const button = screen.getByRole('button')
    expect(button).toBeInTheDocument()
    // Button renders successfully on mobile - icon presence verified in separate test
  })

  it('calls onClick handler when clicked', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()

    vi.mocked(useMediaQuery).mockReturnValue(false)

    renderWithProvider(<CmsEditButton onClick={onClick} />)

    const button = screen.getByRole('button', { name: /edit/i })
    await user.click(button)

    expect(onClick).toHaveBeenCalledTimes(1)
  })

  it('has proper ARIA labels for accessibility', () => {
    vi.mocked(useMediaQuery).mockReturnValue(false)

    const onClick = vi.fn()
    renderWithProvider(<CmsEditButton onClick={onClick} />)

    const button = screen.getByRole('button')
    // Button should have accessible text (either visible text or aria-label)
    expect(button.textContent || button.getAttribute('aria-label')).toBeTruthy()
  })

  it('renders edit icon on mobile', () => {
    vi.mocked(useMediaQuery).mockReturnValue(true)

    const onClick = vi.fn()
    renderWithProvider(<CmsEditButton onClick={onClick} />)

    const button = screen.getByRole('button')
    // Icon should be present (look for svg element)
    const icon = button.querySelector('svg')
    expect(icon).toBeInTheDocument()
  })
})
