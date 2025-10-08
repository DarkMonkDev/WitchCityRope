# Component Implementation Guide: MantineTiptapEditor
<!-- Last Updated: 2025-10-08 -->
<!-- Version: 1.0 -->
<!-- Owner: React Development Team -->
<!-- Status: Active -->

## Purpose
This guide provides complete, copy-paste ready code for implementing the MantineTiptapEditor component as a drop-in replacement for TinyMCERichTextEditor.

## Overview

### Component Features
- ✅ **Drop-in replacement** for TinyMCERichTextEditor (same props interface)
- ✅ **Variable insertion** support with {{fieldName}} autocomplete
- ✅ **Mantine theming** automatic integration
- ✅ **Form integration** works with @mantine/form
- ✅ **Rich formatting** toolbar with all common options
- ✅ **Programmatic control** via imperativeHandle
- ✅ **Type-safe** full TypeScript support

### Props Interface
```typescript
interface MantineTiptapEditorProps {
  value?: string;           // HTML content
  onChange?: (content: string) => void;  // Change handler
  placeholder?: string;     // Placeholder text
  minRows?: number;         // Minimum editor height (rows)
}
```

---

## Complete Component Code

### File: `/apps/web/src/components/forms/MantineTiptapEditor.tsx`

**COPY THIS ENTIRE FILE**:

```typescript
import React, { useEffect, useImperativeHandle, forwardRef } from 'react';
import { RichTextEditor, Link } from '@mantine/tiptap';
import { useEditor } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import Underline from '@tiptap/extension-underline';
import TextAlign from '@tiptap/extension-text-align';
import Superscript from '@tiptap/extension-superscript';
import Subscript from '@tiptap/extension-subscript';
import Highlight from '@tiptap/extension-highlight';
import { Extension } from '@tiptap/core';
import { ReactRenderer } from '@tiptap/react';
import { SuggestionOptions } from '@tiptap/suggestion';
import tippy, { Instance as TippyInstance } from 'tippy.js';
import { Box } from '@mantine/core';

// Variable suggestion list component
interface SuggestionListProps {
  items: string[];
  command: (item: { label: string }) => void;
}

const SuggestionList = React.forwardRef<any, SuggestionListProps>(
  ({ items, command }, ref) => {
    const [selectedIndex, setSelectedIndex] = React.useState(0);

    useImperativeHandle(ref, () => ({
      onKeyDown: ({ event }: { event: KeyboardEvent }) => {
        if (event.key === 'ArrowUp') {
          setSelectedIndex((selectedIndex + items.length - 1) % items.length);
          return true;
        }
        if (event.key === 'ArrowDown') {
          setSelectedIndex((selectedIndex + 1) % items.length);
          return true;
        }
        if (event.key === 'Enter') {
          command({ label: items[selectedIndex] });
          return true;
        }
        return false;
      },
    }));

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
          <Box style={{ padding: '8px 12px', color: '#999' }}>
            No variables available
          </Box>
        )}
      </Box>
    );
  }
);

SuggestionList.displayName = 'SuggestionList';

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
          ];

          return variables
            .filter(item =>
              item.toLowerCase().includes(query.toLowerCase())
            )
            .slice(0, 10);
        },
        render: () => {
          let component: ReactRenderer<any>;
          let popup: TippyInstance[];

          return {
            onStart: (props: any) => {
              component = new ReactRenderer(SuggestionList, {
                props,
                editor: props.editor,
              });

              if (!props.clientRect) {
                return;
              }

              popup = tippy('body', {
                getReferenceClientRect: props.clientRect,
                appendTo: () => document.body,
                content: component.element,
                showOnCreate: true,
                interactive: true,
                trigger: 'manual',
                placement: 'bottom-start',
              });
            },
            onUpdate(props: any) {
              component.updateProps(props);

              if (!props.clientRect) {
                return;
              }

              popup[0].setProps({
                getReferenceClientRect: props.clientRect,
              });
            },
            onKeyDown(props: any) {
              if (props.event.key === 'Escape') {
                popup[0].hide();
                return true;
              }
              return component.ref?.onKeyDown(props);
            },
            onExit() {
              popup[0].destroy();
              component.destroy();
            },
          };
        },
        command: ({ editor, range, props }: any) => {
          editor
            .chain()
            .focus()
            .deleteRange(range)
            .insertContent(`{{${props.label}}}`)
            .run();
        },
      } as Partial<SuggestionOptions>,
    };
  },
});

// Main component props
export interface MantineTiptapEditorProps {
  value?: string;
  onChange?: (content: string) => void;
  placeholder?: string;
  minRows?: number;
}

export interface MantineTiptapEditorRef {
  getHTML: () => string;
  setContent: (content: string) => void;
  focus: () => void;
  clear: () => void;
}

// Main component
export const MantineTiptapEditor = forwardRef<
  MantineTiptapEditorRef,
  MantineTiptapEditorProps
>(({ value = '', onChange, placeholder = 'Enter text...', minRows = 4 }, ref) => {
  const editor = useEditor({
    extensions: [
      StarterKit,
      Underline,
      Link,
      Superscript,
      Subscript,
      Highlight,
      TextAlign.configure({ types: ['heading', 'paragraph'] }),
      VariableInsertion,
    ],
    content: value,
    onUpdate: ({ editor }) => {
      const html = editor.getHTML();
      onChange?.(html);
    },
    editorProps: {
      attributes: {
        style: `min-height: ${minRows * 30}px;`,
      },
    },
  });

  // Update editor content when value prop changes
  useEffect(() => {
    if (editor && value !== editor.getHTML()) {
      editor.commands.setContent(value);
    }
  }, [value, editor]);

  // Expose imperative methods via ref
  useImperativeHandle(ref, () => ({
    getHTML: () => editor?.getHTML() ?? '',
    setContent: (content: string) => {
      editor?.commands.setContent(content);
    },
    focus: () => {
      editor?.commands.focus();
    },
    clear: () => {
      editor?.commands.clearContent();
    },
  }));

  if (!editor) {
    return null;
  }

  return (
    <RichTextEditor editor={editor} placeholder={placeholder}>
      <RichTextEditor.Toolbar sticky stickyOffset={60}>
        <RichTextEditor.ControlsGroup>
          <RichTextEditor.Bold />
          <RichTextEditor.Italic />
          <RichTextEditor.Underline />
          <RichTextEditor.Strikethrough />
          <RichTextEditor.ClearFormatting />
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
  );
});

MantineTiptapEditor.displayName = 'MantineTiptapEditor';
```

---

## Required Dependencies

**GOOD NEWS**: All dependencies already installed in package.json!

```json
{
  "@mantine/tiptap": "^7.17.8",
  "@tiptap/extension-highlight": "^3.3.0",
  "@tiptap/extension-link": "^3.3.0",
  "@tiptap/extension-subscript": "^3.3.0",
  "@tiptap/extension-superscript": "^3.3.0",
  "@tiptap/extension-text-align": "^3.3.0",
  "@tiptap/extension-underline": "^3.3.0",
  "@tiptap/react": "^3.3.0",
  "@tiptap/starter-kit": "^3.3.0"
}
```

**Additional dependency needed** (for suggestion popups):

```bash
npm install tippy.js
```

---

## Usage Examples

### Example 1: Basic Usage in EventForm

**File**: `/apps/web/src/components/events/EventForm.tsx`

**OLD CODE (Remove)**:
```typescript
import { Editor } from '@tinymce/tinymce-react';

const tinyMCEApiKey = import.meta.env.VITE_TINYMCE_API_KEY;
const shouldUseTinyMCE = !!tinyMCEApiKey;

const RichTextEditor: React.FC<{...}> = ({ value, onChange, height, placeholder }) => {
  if (!shouldUseTinyMCE) {
    return <Textarea ... />
  }
  return <Editor apiKey={tinyMCEApiKey} ... />
};
```

**NEW CODE (Replace with)**:
```typescript
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor';

// Remove: tinyMCEApiKey, shouldUseTinyMCE, RichTextEditor component

// Use directly in JSX:
<MantineTiptapEditor
  value={form.values.fullDescription}
  onChange={(content) => form.setFieldValue('fullDescription', content)}
  placeholder="Describe the event in detail..."
  minRows={6}
/>
```

### Example 2: With @mantine/form Integration

```typescript
import { useForm } from '@mantine/form';
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor';

function EventDescriptionForm() {
  const form = useForm({
    initialValues: {
      description: '<p>Initial HTML content</p>',
    },
    validate: {
      description: (value) =>
        value.length < 10 ? 'Description too short' : null,
    },
  });

  return (
    <form onSubmit={form.onSubmit((values) => console.log(values))}>
      <MantineTiptapEditor
        value={form.values.description}
        onChange={(content) => form.setFieldValue('description', content)}
        placeholder="Enter event description..."
        minRows={8}
      />

      {form.errors.description && (
        <Text c="red" size="sm">{form.errors.description}</Text>
      )}

      <Button type="submit">Save Event</Button>
    </form>
  );
}
```

### Example 3: Using Ref for Programmatic Control

```typescript
import { useRef } from 'react';
import { MantineTiptapEditor, MantineTiptapEditorRef } from '../forms/MantineTiptapEditor';

function ProgrammaticEditorExample() {
  const editorRef = useRef<MantineTiptapEditorRef>(null);

  const insertTemplate = () => {
    const template = `
      <h2>Event Details</h2>
      <p>Date: {{eventDate}}</p>
      <p>Location: {{eventLocation}}</p>
      <p>Instructor: {{teacherName}}</p>
    `;
    editorRef.current?.setContent(template);
  };

  const clearContent = () => {
    editorRef.current?.clear();
  };

  const focusEditor = () => {
    editorRef.current?.focus();
  };

  return (
    <div>
      <Group mb="md">
        <Button onClick={insertTemplate}>Insert Template</Button>
        <Button onClick={clearContent} color="red">Clear</Button>
        <Button onClick={focusEditor}>Focus</Button>
      </Group>

      <MantineTiptapEditor
        ref={editorRef}
        placeholder="Type or insert template..."
        minRows={10}
      />
    </div>
  );
}
```

### Example 4: Variable Insertion Testing

```typescript
function VariableInsertionDemo() {
  const [content, setContent] = useState('');

  return (
    <div>
      <MantineTiptapEditor
        value={content}
        onChange={setContent}
        placeholder="Try typing {{ to insert variables..."
        minRows={6}
      />

      <Text mt="md" size="sm" c="dimmed">
        Tip: Type "{{" to see available variables
      </Text>

      {/* Preview HTML output */}
      <Paper mt="md" p="md" withBorder>
        <Text fw={700} mb="xs">HTML Output:</Text>
        <Code block>{content}</Code>
      </Paper>
    </div>
  );
}
```

---

## Variable Insertion Extension

### How It Works

1. **Trigger**: User types `{{`
2. **Autocomplete Menu**: Shows available variables
3. **Navigation**: Arrow keys to select, Enter to insert
4. **Output**: Inserts `{{variableName}}` into content

### Available Variables (Default)

```typescript
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
];
```

### Customizing Variable List

**Option 1**: Modify default list in component

```typescript
// In VariableInsertion extension, update items array:
items: ({ query }: { query: string }) => {
  const variables = [
    'customVariable1',
    'customVariable2',
    // ... your variables
  ];
  return variables.filter(item =>
    item.toLowerCase().includes(query.toLowerCase())
  );
},
```

**Option 2**: Pass variables as prop (requires component modification)

```typescript
// Add to component props:
export interface MantineTiptapEditorProps {
  value?: string;
  onChange?: (content: string) => void;
  placeholder?: string;
  minRows?: number;
  availableVariables?: string[];  // NEW
}

// Update VariableInsertion to use prop:
// (requires refactoring extension to accept dynamic variables)
```

### Testing Variable Insertion

```typescript
test('Variable insertion autocomplete works', async () => {
  render(<MantineTiptapEditor />);

  const editor = screen.getByRole('textbox');

  // Type trigger characters
  await userEvent.type(editor, '{{');

  // Verify autocomplete menu appears
  expect(screen.getByText('eventTitle')).toBeInTheDocument();
  expect(screen.getByText('userName')).toBeInTheDocument();

  // Select variable
  await userEvent.click(screen.getByText('eventTitle'));

  // Verify insertion
  expect(editor).toHaveTextContent('{{eventTitle}}');
});
```

---

## Integration Checklist

### Step 1: Create Component File
- [ ] Create `/apps/web/src/components/forms/MantineTiptapEditor.tsx`
- [ ] Copy complete component code from this guide
- [ ] Install tippy.js: `npm install tippy.js`
- [ ] Verify TypeScript compiles: `npx tsc --noEmit`

### Step 2: Update EventForm
- [ ] Open `/apps/web/src/components/events/EventForm.tsx`
- [ ] Remove TinyMCE import: `import { Editor } from '@tinymce/tinymce-react'`
- [ ] Add new import: `import { MantineTiptapEditor } from '../forms/MantineTiptapEditor'`
- [ ] Remove `tinyMCEApiKey` and `shouldUseTinyMCE` variables
- [ ] Remove `RichTextEditor` inline component
- [ ] Replace all `<RichTextEditor />` with `<MantineTiptapEditor />`

### Step 3: Update All Rich Text Editor Usage
- [ ] Find all TinyMCE usage: `grep -r "TinyMCERichTextEditor" apps/web/src/`
- [ ] Replace with MantineTiptapEditor component
- [ ] Update imports in all files
- [ ] Verify props match (value, onChange, placeholder, minRows)

### Step 4: Test Integration
- [ ] TypeScript compilation: `npx tsc --noEmit` (should show 0 errors)
- [ ] Build: `npm run build` (should succeed)
- [ ] Start dev server: `./dev.sh`
- [ ] Navigate to event creation page
- [ ] Verify editor renders correctly
- [ ] Test text input and formatting
- [ ] Test variable insertion (type `{{`)
- [ ] Test form submission

### Step 5: Verify Form Integration
- [ ] Enter content in editor
- [ ] Submit form
- [ ] Verify HTML saved correctly
- [ ] Load saved content
- [ ] Verify content displays in editor

---

## Testing Instructions

### Manual Testing Procedure

1. **Start Development Environment**
   ```bash
   ./dev.sh
   # Wait for containers to be ready
   ```

2. **Navigate to Event Creation**
   ```
   http://localhost:5174/admin/events
   Click "Create Event" button
   ```

3. **Test Basic Functionality**
   - [ ] Editor renders with toolbar
   - [ ] Can type text
   - [ ] Bold/italic/underline buttons work
   - [ ] Heading buttons work
   - [ ] Lists work (bullet and ordered)
   - [ ] Undo/redo work

4. **Test Variable Insertion**
   - [ ] Type `{{` triggers autocomplete
   - [ ] List shows available variables
   - [ ] Arrow keys navigate list
   - [ ] Enter key inserts variable
   - [ ] Variable appears as `{{variableName}}`

5. **Test Form Integration**
   - [ ] Enter content in editor
   - [ ] Fill other form fields
   - [ ] Click "Save"
   - [ ] Verify success message
   - [ ] Edit saved event
   - [ ] Verify content loads correctly

6. **Test Edge Cases**
   - [ ] Empty editor (should allow save)
   - [ ] Very long content (>10,000 chars)
   - [ ] HTML entities (<, >, &)
   - [ ] Paste from Word (formatting preserved)
   - [ ] Multiple editors on same page

### Automated Testing

**Unit Test** (create: `/apps/web/src/components/forms/MantineTiptapEditor.test.tsx`):

```typescript
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MantineTiptapEditor } from './MantineTiptapEditor';

describe('MantineTiptapEditor', () => {
  test('renders editor with placeholder', () => {
    render(<MantineTiptapEditor placeholder="Test placeholder" />);
    expect(screen.getByPlaceholderText('Test placeholder')).toBeInTheDocument();
  });

  test('calls onChange when content changes', async () => {
    const handleChange = vi.fn();
    render(<MantineTiptapEditor onChange={handleChange} />);

    const editor = screen.getByRole('textbox');
    await userEvent.type(editor, 'Test content');

    expect(handleChange).toHaveBeenCalled();
    expect(handleChange).toHaveBeenCalledWith(
      expect.stringContaining('Test content')
    );
  });

  test('updates content when value prop changes', () => {
    const { rerender } = render(
      <MantineTiptapEditor value="<p>Initial</p>" />
    );

    expect(screen.getByText('Initial')).toBeInTheDocument();

    rerender(<MantineTiptapEditor value="<p>Updated</p>" />);

    expect(screen.getByText('Updated')).toBeInTheDocument();
  });
});
```

**E2E Test** (create: `/apps/web/tests/playwright/tiptap-editor.spec.ts`):

See [testing-migration-guide.md](./testing-migration-guide.md) for complete E2E test code.

---

## Troubleshooting

### Issue: TypeScript Errors

**Error**: `Cannot find module '@mantine/tiptap'`

**Solution**:
```bash
npm list @mantine/tiptap
# If not found:
npm install @mantine/tiptap
```

**Error**: `Cannot find module 'tippy.js'`

**Solution**:
```bash
npm install tippy.js
```

**Error**: `Property 'ref' does not exist on type...`

**Solution**: Ensure SuggestionList is properly forwardRef wrapped and useImperativeHandle is used.

### Issue: Variable Insertion Not Working

**Symptom**: Typing `{{` doesn't show autocomplete menu

**Debug Steps**:
1. Check browser console for errors
2. Verify VariableInsertion extension is registered
3. Test with simple variable list first
4. Check tippy.js is installed and imported

**Fix**: Verify extension configuration:
```typescript
const editor = useEditor({
  extensions: [
    // ... other extensions
    VariableInsertion,  // Must be included
  ],
});
```

### Issue: Editor Not Rendering

**Symptom**: Blank space where editor should be

**Debug Steps**:
1. Check browser console for errors
2. Verify @mantine/tiptap CSS is loaded
3. Check editor initialization: `console.log(editor)`
4. Verify parent container has height

**Fix**: Check Mantine provider wraps component:
```typescript
// In App.tsx or _app.tsx
import { MantineProvider } from '@mantine/core';

<MantineProvider>
  <YourComponent />
</MantineProvider>
```

### Issue: Styling Doesn't Match Mantine Theme

**Symptom**: Editor looks different from rest of app

**Fix**: Ensure MantineProvider is properly configured:
```typescript
import { MantineProvider, createTheme } from '@mantine/core';
import '@mantine/core/styles.css';
import '@mantine/tiptap/styles.css';

const theme = createTheme({
  // Your theme config
});

<MantineProvider theme={theme}>
  <App />
</MantineProvider>
```

### Issue: Form Values Not Updating

**Symptom**: Editor changes don't save

**Debug**: Add console.log to onChange:
```typescript
<MantineTiptapEditor
  value={form.values.description}
  onChange={(content) => {
    console.log('Editor changed:', content);
    form.setFieldValue('description', content);
  }}
/>
```

**Fix**: Ensure onChange properly connected to form state.

### Issue: Editor Performance Slow

**Symptom**: Typing lags or editor freezes

**Solutions**:
1. **Debounce onChange**:
   ```typescript
   import { useDebouncedValue } from '@mantine/hooks';

   const [editorValue, setEditorValue] = useState(initialValue);
   const [debouncedValue] = useDebouncedValue(editorValue, 300);

   useEffect(() => {
     onChange?.(debouncedValue);
   }, [debouncedValue]);
   ```

2. **Optimize extensions**: Remove unused extensions
3. **Lazy load editor**: Use React.lazy() for code splitting

---

## Comparison with TinyMCE

### Props Mapping

| TinyMCE Prop | MantineTiptap Equivalent | Notes |
|--------------|--------------------------|-------|
| `apiKey` | *(none)* | No API key needed |
| `value` | `value` | Same |
| `onEditorChange` | `onChange` | Same signature |
| `init.height` | `minRows` | Convert: height/30 = rows |
| `init.placeholder` | `placeholder` | Same |
| `init.plugins` | Tiptap extensions | Import as needed |
| `init.toolbar` | RichTextEditor.Toolbar | Declarative components |
| `init.content_style` | *(automatic)* | Uses Mantine theme |

### Feature Parity

| Feature | TinyMCE | MantineTiptap | Status |
|---------|---------|---------------|--------|
| **Basic Formatting** | ✅ | ✅ | Full parity |
| **Headings** | ✅ | ✅ | Full parity |
| **Lists** | ✅ | ✅ | Full parity |
| **Links** | ✅ | ✅ | Full parity |
| **Alignment** | ✅ | ✅ | Full parity |
| **Undo/Redo** | ✅ | ✅ | Full parity |
| **Tables** | ✅ | ⚠️ | Requires @tiptap/extension-table |
| **Images** | ✅ | ⚠️ | Requires custom implementation |
| **Variable Insertion** | ❌ | ✅ | **Tiptap advantage** |
| **API Key Required** | ✅ | ❌ | **Tiptap advantage** |
| **Bundle Size** | 500KB+ | ~155KB | **Tiptap advantage** |

### Migration Effort by Use Case

| Use Case | Effort | Notes |
|----------|--------|-------|
| **Simple text editor** | 15 min | Drop-in replacement |
| **With variable insertion** | 30 min | Copy custom extension |
| **Form integration** | 15 min | Same onChange pattern |
| **Multiple editors** | 30 min | Repeat for each instance |
| **Custom toolbar** | 1-2 hours | Rebuild with Tiptap components |

---

## Next Steps

1. **Create Component**: Copy code to MantineTiptapEditor.tsx
2. **Install Dependencies**: `npm install tippy.js`
3. **Update EventForm**: Replace TinyMCE usage
4. **Test Locally**: Verify functionality
5. **Update Tests**: See [testing-migration-guide.md](./testing-migration-guide.md)
6. **Deploy**: Follow [migration-plan.md](./migration-plan.md)

---

## Related Documentation

- **Migration Plan**: [migration-plan.md](./migration-plan.md) - Complete 5-phase migration
- **Testing Guide**: [testing-migration-guide.md](./testing-migration-guide.md) - E2E test updates
- **Configuration Guide**: [configuration-cleanup-guide.md](./configuration-cleanup-guide.md) - Config cleanup
- **Rollback Plan**: [rollback-plan.md](./rollback-plan.md) - Emergency procedures

---

## Version History
- **v1.0** (2025-10-08): Initial implementation guide created

---

**Questions?** See [troubleshooting section](#troubleshooting) or consult [migration-plan.md](./migration-plan.md).
