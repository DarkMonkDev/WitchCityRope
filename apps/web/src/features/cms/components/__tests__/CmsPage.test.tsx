import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { CmsPage } from '../CmsPage'
import * as authStore from '../../../../stores/authStore'
import * as useCmsPageHook from '../../hooks/useCmsPage'

// Mock dependencies
vi.mock('../../../../stores/authStore')
vi.mock('../../hooks/useCmsPage')
vi.mock('../../../../components/forms/MantineTiptapEditor', () => ({
  MantineTiptapEditor: ({ value, onChange }: any) => (
    <div
      data-testid="tiptap-editor"
      contentEditable
      onInput={(e) => onChange((e.target as HTMLElement).textContent || '')}
    >
      {value}
    </div>
  ),
}))

describe('CmsPage Component', () => {
  let queryClient: QueryClient

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    })
  })

  const mockCmsContent = {
    id: 1,
    slug: 'resources',
    title: 'Community Resources',
    content: '<h1>Resources</h1><p>Content here</p>',
    updatedAt: '2025-10-17T12:00:00Z',
    lastModifiedBy: 'admin@witchcityrope.com',
  }

  const mockUseCmsPage = {
    content: mockCmsContent,
    isLoading: false,
    save: vi.fn(),
    isSaving: false,
    error: null,
  }

  it('renders content in view mode for non-admin users', () => {
    // Mock non-admin user
    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Member' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue(mockUseCmsPage as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Verify content is displayed
    expect(screen.getByText(/Resources/i)).toBeInTheDocument()

    // Verify edit button is NOT visible for non-admin
    expect(screen.queryByRole('button', { name: /edit/i })).not.toBeInTheDocument()
  })

  it('shows edit button for admin users', () => {
    // Mock admin user
    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Administrator' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue(mockUseCmsPage as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Verify edit button is visible for admin
    expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument()
  })

  it('switches to edit mode when edit button clicked', async () => {
    const user = userEvent.setup()

    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Administrator' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue(mockUseCmsPage as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Click edit button
    const editButton = screen.getByRole('button', { name: /edit/i })
    await user.click(editButton)

    // Verify edit mode activated
    expect(screen.getByTestId('tiptap-editor')).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: /page title/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /save/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument()
  })

  it('tracks dirty state when content changes', async () => {
    const user = userEvent.setup()

    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Administrator' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue(mockUseCmsPage as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Enter edit mode
    await user.click(screen.getByRole('button', { name: /edit/i }))

    // Initially, save button should be disabled (not dirty)
    expect(screen.getByRole('button', { name: /save/i })).toBeDisabled()

    // Change title
    const titleInput = screen.getByRole('textbox', { name: /page title/i })
    await user.type(titleInput, ' Updated')

    // Now save button should be enabled (dirty)
    expect(screen.getByRole('button', { name: /save/i })).not.toBeDisabled()
  })

  it('calls save mutation when save button clicked', async () => {
    const user = userEvent.setup()
    const mockSave = vi.fn().mockResolvedValue({})

    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Administrator' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue({
      ...mockUseCmsPage,
      save: mockSave,
    } as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Enter edit mode
    await user.click(screen.getByRole('button', { name: /edit/i }))

    // Make changes
    const titleInput = screen.getByRole('textbox', { name: /page title/i })
    await user.clear(titleInput)
    await user.type(titleInput, 'Updated Title')

    // Save
    await user.click(screen.getByRole('button', { name: /save/i }))

    // Verify save was called
    await waitFor(() => {
      expect(mockSave).toHaveBeenCalledWith({
        title: 'Updated Title',
        content: expect.any(String),
      })
    })
  })

  it('shows cancel modal when canceling with unsaved changes', async () => {
    const user = userEvent.setup()

    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Administrator' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue(mockUseCmsPage as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Enter edit mode
    await user.click(screen.getByRole('button', { name: /edit/i }))

    // Make changes
    const titleInput = screen.getByRole('textbox', { name: /page title/i })
    await user.type(titleInput, ' Changed')

    // Click cancel
    await user.click(screen.getByRole('button', { name: /cancel/i }))

    // Verify modal appears
    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument()
      expect(screen.getByText(/unsaved changes/i)).toBeInTheDocument()
    })
  })

  it('discards changes when confirmed in modal', async () => {
    const user = userEvent.setup()

    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Administrator' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue(mockUseCmsPage as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Enter edit mode and make changes
    await user.click(screen.getByRole('button', { name: /edit/i }))
    const titleInput = screen.getByRole('textbox', { name: /page title/i })
    await user.type(titleInput, ' Changed')

    // Cancel
    await user.click(screen.getByRole('button', { name: /cancel/i }))

    // Confirm discard in modal
    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    const discardButton = screen.getByRole('button', { name: /discard/i })
    await user.click(discardButton)

    // Verify editor closed
    await waitFor(() => {
      expect(screen.queryByTestId('tiptap-editor')).not.toBeInTheDocument()
    })
  })

  it('shows loading state while fetching content', () => {
    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Administrator' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue({
      ...mockUseCmsPage,
      isLoading: true,
      content: null,
    } as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Verify loading overlay is shown
    expect(screen.getByTestId('mantine-LoadingOverlay-overlay') || screen.getByRole('status')).toBeInTheDocument()
  })

  it('shows error state when content fails to load', () => {
    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Administrator' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue({
      ...mockUseCmsPage,
      content: null,
      error: new Error('Failed to fetch'),
    } as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage slug="resources" />
      </QueryClientProvider>
    )

    // Verify error message is shown
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText(/failed to load/i)).toBeInTheDocument()
  })

  it('renders default content when provided and no fetched content', () => {
    vi.mocked(authStore.useUser).mockReturnValue({ role: 'Member' } as any)
    vi.mocked(useCmsPageHook.useCmsPage).mockReturnValue({
      ...mockUseCmsPage,
      content: null,
    } as any)

    render(
      <QueryClientProvider client={queryClient}>
        <CmsPage
          slug="resources"
          defaultTitle="Default Title"
          defaultContent="<p>Default content here</p>"
        />
      </QueryClientProvider>
    )

    // Verify default content is rendered
    expect(screen.getByText(/Default content here/i)).toBeInTheDocument()
  })
})
