import React, { useEffect, useImperativeHandle, forwardRef } from 'react'
import { RichTextEditor } from '@mantine/tiptap'
import { useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
// Note: Link and Underline are automatically added by Mantine's RichTextEditor toolbar controls
// Importing them here causes duplicate extension warnings
import TextAlign from '@tiptap/extension-text-align'
import Superscript from '@tiptap/extension-superscript'
import Subscript from '@tiptap/extension-subscript'
import Highlight from '@tiptap/extension-highlight'
import { TextStyle } from '@tiptap/extension-text-style'
import { Color } from '@tiptap/extension-color'
import { Extension } from '@tiptap/core'
import { ReactRenderer } from '@tiptap/react'
import { SuggestionOptions } from '@tiptap/suggestion'
import tippy, { Instance as TippyInstance } from 'tippy.js'
import { Box } from '@mantine/core'

// Variable suggestion list component
interface SuggestionListProps {
  items: string[]
  command: (item: { label: string }) => void
}

const SuggestionList = React.forwardRef<any, SuggestionListProps>(({ items, command }, ref) => {
  const [selectedIndex, setSelectedIndex] = React.useState(0)

  useImperativeHandle(ref, () => ({
    onKeyDown: ({ event }: { event: KeyboardEvent }) => {
      if (event.key === 'ArrowUp') {
        setSelectedIndex((selectedIndex + items.length - 1) % items.length)
        return true
      }
      if (event.key === 'ArrowDown') {
        setSelectedIndex((selectedIndex + 1) % items.length)
        return true
      }
      if (event.key === 'Enter') {
        command({ label: items[selectedIndex] })
        return true
      }
      return false
    },
  }))

  return (
    <Box
      style={{
        background: 'white',
        border: '1px solid #ddd',
        borderRadius: '4px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
        padding: '4px',
        maxHeight: '200px',
        overflow: 'auto',
      }}
    >
      {items.length > 0 ? (
        items.map((item, index) => (
          <Box
            key={item}
            onClick={() => command({ label: item })}
            style={{
              padding: '8px 12px',
              cursor: 'pointer',
              borderRadius: '2px',
              background: index === selectedIndex ? '#f0f0f0' : 'transparent',
              fontFamily: 'monospace',
            }}
            onMouseEnter={() => setSelectedIndex(index)}
          >
            {`{{${item}}}`}
          </Box>
        ))
      ) : (
        <Box style={{ padding: '8px 12px', color: '#999' }}>No variables available</Box>
      )}
    </Box>
  )
})

SuggestionList.displayName = 'SuggestionList'

// Variable insertion custom extension
const VariableInsertion = Extension.create({
  name: 'variableInsertion',

  addOptions() {
    return {
      suggestion: {
        char: '{{',
        allowSpaces: true,
        allowedPrefixes: null,
        startOfLine: false,
        items: ({ query }: { query: string }) => {
          // Available variables (customize for your use case)
          const variables = [
            'eventTitle',
            'eventDate',
            'eventLocation',
            'userName',
            'userEmail',
            'ticketType',
            'ticketPrice',
            'registrationDate',
            'confirmationNumber',
          ]

          return variables
            .filter((item) => item.toLowerCase().includes(query.toLowerCase()))
            .slice(0, 10)
        },
        render: () => {
          let component: ReactRenderer<any>
          let popup: TippyInstance[]

          return {
            onStart: (props: any) => {
              component = new ReactRenderer(SuggestionList, {
                props,
                editor: props.editor,
              })

              if (!props.clientRect) {
                return
              }

              popup = tippy('body', {
                getReferenceClientRect: props.clientRect,
                appendTo: () => document.body,
                content: component.element,
                showOnCreate: true,
                interactive: true,
                trigger: 'manual',
                placement: 'bottom-start',
              })
            },
            onUpdate(props: any) {
              component.updateProps(props)

              if (!props.clientRect) {
                return
              }

              popup[0].setProps({
                getReferenceClientRect: props.clientRect,
              })
            },
            onKeyDown(props: any) {
              if (props.event.key === 'Escape') {
                popup[0].hide()
                return true
              }
              return component.ref?.onKeyDown(props)
            },
            onExit() {
              popup[0].destroy()
              component.destroy()
            },
          }
        },
        command: ({ editor, range, props }: any) => {
          editor.chain().focus().deleteRange(range).insertContent(`{{${props.label}}}`).run()
        },
      } as Partial<SuggestionOptions>,
    }
  },
})

// Main component props
export interface MantineTiptapEditorProps {
  value?: string
  onChange?: (content: string) => void
  placeholder?: string
  minRows?: number
}

export interface MantineTiptapEditorRef {
  getHTML: () => string
  setContent: (content: string) => void
  focus: () => void
  clear: () => void
}

// Main component
export const MantineTiptapEditor = forwardRef<MantineTiptapEditorRef, MantineTiptapEditorProps>(
  ({ value = '', onChange, placeholder = 'Enter text...', minRows = 4 }, ref) => {
    const editor = useEditor({
      extensions: [
        StarterKit,
        // Note: Link and Underline extensions are automatically added by Mantine's
        // RichTextEditor.Link and RichTextEditor.Underline toolbar controls
        // We don't need to add them here - doing so causes duplicate extension warnings
        TextStyle, // Required for Color extension
        Color,
        Superscript,
        Subscript,
        Highlight,
        TextAlign.configure({ types: ['heading', 'paragraph'] }),
        VariableInsertion,
      ],
      content: value,
      onUpdate: ({ editor }) => {
        const html = editor.getHTML()
        onChange?.(html)
      },
      editorProps: {
        attributes: {
          style: `min-height: ${minRows * 30}px;`,
          'data-placeholder': placeholder,
        },
      },
    })

    // Update editor content when value prop changes
    // Only update if the value is significantly different from current content
    useEffect(() => {
      if (!editor) return

      const currentContent = editor.getHTML()

      // Normalize HTML for comparison (remove extra whitespace, normalize tags)
      const normalize = (html: string) => html?.trim().replace(/\s+/g, ' ') || ''
      const normalizedValue = normalize(value)
      const normalizedCurrent = normalize(currentContent)

      // Only update if content has actually changed
      if (normalizedValue !== normalizedCurrent) {
        // Prevent cursor jump by checking if editor is focused
        const isFocused = editor.isFocused

        editor.commands.setContent(value)

        // Restore focus if editor was focused
        if (isFocused) {
          editor.commands.focus('end')
        }
      }
    }, [value, editor])

    // Expose imperative methods via ref
    useImperativeHandle(ref, () => ({
      getHTML: () => editor?.getHTML() ?? '',
      setContent: (content: string) => {
        editor?.commands.setContent(content)
      },
      focus: () => {
        editor?.commands.focus()
      },
      clear: () => {
        editor?.commands.clearContent()
      },
    }))

    if (!editor) {
      return null
    }

    return (
      <RichTextEditor editor={editor}>
        <RichTextEditor.Toolbar sticky stickyOffset={60}>
          <RichTextEditor.ControlsGroup>
            <RichTextEditor.Bold />
            <RichTextEditor.Italic />
            <RichTextEditor.Underline />
            <RichTextEditor.Strikethrough />
            <RichTextEditor.ClearFormatting />
          </RichTextEditor.ControlsGroup>

          <RichTextEditor.ControlsGroup>
            <RichTextEditor.ColorPicker
              colors={[
                '#000000',
                '#880124',
                '#C41E3A',
                '#FF6B35',
                '#F7931E',
                '#2E7D32',
                '#1976D2',
                '#7B1FA2',
                '#616161',
                '#FFFFFF',
              ]}
            />
          </RichTextEditor.ControlsGroup>

          <RichTextEditor.ControlsGroup>
            <RichTextEditor.H1 />
            <RichTextEditor.H2 />
            <RichTextEditor.H3 />
            <RichTextEditor.H4 />
          </RichTextEditor.ControlsGroup>

          <RichTextEditor.ControlsGroup>
            <RichTextEditor.Blockquote />
            <RichTextEditor.Hr />
            <RichTextEditor.BulletList />
            <RichTextEditor.OrderedList />
          </RichTextEditor.ControlsGroup>

          <RichTextEditor.ControlsGroup>
            <RichTextEditor.Link />
            <RichTextEditor.Unlink />
          </RichTextEditor.ControlsGroup>

          <RichTextEditor.ControlsGroup>
            <RichTextEditor.AlignLeft />
            <RichTextEditor.AlignCenter />
            <RichTextEditor.AlignJustify />
            <RichTextEditor.AlignRight />
          </RichTextEditor.ControlsGroup>

          <RichTextEditor.ControlsGroup>
            <RichTextEditor.Undo />
            <RichTextEditor.Redo />
          </RichTextEditor.ControlsGroup>
        </RichTextEditor.Toolbar>

        <RichTextEditor.Content />
      </RichTextEditor>
    )
  }
)

MantineTiptapEditor.displayName = 'MantineTiptapEditor'
