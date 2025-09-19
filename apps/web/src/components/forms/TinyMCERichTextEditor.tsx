import React from 'react';
import { Editor } from '@tinymce/tinymce-react';
import { Alert, Box, Textarea } from '@mantine/core';

interface TinyMCERichTextEditorProps {
  value?: string;
  onChange?: (content: string) => void;
  placeholder?: string;
  minRows?: number;
}

export const TinyMCERichTextEditor: React.FC<TinyMCERichTextEditorProps> = ({
  value = '',
  onChange,
  placeholder = 'Enter text...',
  minRows = 4
}) => {
  const apiKey = import.meta.env.VITE_TINYMCE_API_KEY;

  if (!apiKey) {
    return (
      <Box>
        <Alert color="blue" mb="xs" title="Development Mode">
          TinyMCE disabled to prevent API usage costs. Using simple text editor.
        </Alert>
        <Textarea
          value={value}
          onChange={(event) => onChange?.(event.target.value)}
          minRows={minRows}
          placeholder={placeholder}
          autosize
          styles={{
            input: {
              fontFamily: 'Source Sans 3, sans-serif',
              fontSize: '14px',
              lineHeight: '1.6'
            }
          }}
        />
      </Box>
    );
  }

  return (
    <Editor
      apiKey={apiKey}
      value={value}
      onEditorChange={(newContent) => {
        if (onChange) {
          onChange(newContent);
        }
      }}
      init={{
        height: minRows ? minRows * 30 : 300,
        menubar: false,
        plugins: [
          'anchor', 'autolink', 'charmap', 'codesample', 'emoticons', 
          'link', 'lists', 'media', 'searchreplace', 'table', 
          'visualblocks', 'wordcount'
        ],
        toolbar: 'undo redo | blocks fontfamily fontsize | ' +
          'bold italic underline strikethrough | link media table | ' +
          'align lineheight | numlist bullist indent outdent | ' +
          'emoticons charmap | removeformat',
        content_style: `
          body { 
            font-family: 'Source Sans 3', sans-serif; 
            font-size: 14px;
            color: #2B2B2B;
          }
        `,
        placeholder: placeholder,
        branding: false, // Remove TinyMCE branding
        promotion: false, // Remove upgrade promotions
        statusbar: true, // Show word count
        resize: true, // Allow vertical resize
        contextmenu: false, // Disable right-click menu
      }}
    />
  );
};