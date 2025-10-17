import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { CmsCancelModal } from '../CmsCancelModal'

describe('CmsCancelModal Component', () => {
  it('renders when opened is true', () => {
    const onClose = vi.fn()
    const onConfirm = vi.fn()

    render(<CmsCancelModal opened={true} onClose={onClose} onConfirm={onConfirm} />)

    // Verify modal is visible
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText(/unsaved changes/i)).toBeInTheDocument()
  })

  it('does not render when opened is false', () => {
    const onClose = vi.fn()
    const onConfirm = vi.fn()

    render(<CmsCancelModal opened={false} onClose={onClose} onConfirm={onConfirm} />)

    // Verify modal is not visible
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('calls onClose when "Keep Editing" is clicked', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    const onConfirm = vi.fn()

    render(<CmsCancelModal opened={true} onClose={onClose} onConfirm={onConfirm} />)

    const keepEditingButton = screen.getByRole('button', { name: /keep editing/i })
    await user.click(keepEditingButton)

    expect(onClose).toHaveBeenCalledTimes(1)
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('calls onConfirm when "Discard Changes" is clicked', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    const onConfirm = vi.fn()

    render(<CmsCancelModal opened={true} onClose={onClose} onConfirm={onConfirm} />)

    const discardButton = screen.getByRole('button', { name: /discard/i })
    await user.click(discardButton)

    expect(onConfirm).toHaveBeenCalledTimes(1)
  })

  it('displays warning message about unsaved changes', () => {
    const onClose = vi.fn()
    const onConfirm = vi.fn()

    render(<CmsCancelModal opened={true} onClose={onClose} onConfirm={onConfirm} />)

    // Verify warning text is present
    expect(screen.getByText(/are you sure you want to discard/i)).toBeInTheDocument()
  })

  it('does not close on backdrop click (closeOnClickOutside should be false)', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    const onConfirm = vi.fn()

    render(<CmsCancelModal opened={true} onClose={onClose} onConfirm={onConfirm} />)

    // Click outside modal (on backdrop)
    const backdrop = screen.getByRole('dialog').parentElement
    if (backdrop) {
      await user.click(backdrop)
    }

    // onClose should NOT be called (modal prevents backdrop close)
    expect(onClose).not.toHaveBeenCalled()
  })

  it('renders both action buttons', () => {
    const onClose = vi.fn()
    const onConfirm = vi.fn()

    render(<CmsCancelModal opened={true} onClose={onClose} onConfirm={onConfirm} />)

    // Verify both buttons are present
    expect(screen.getByRole('button', { name: /keep editing/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /discard/i })).toBeInTheDocument()
  })
})
