import React, { useState, useEffect } from 'react';
import { Editor } from '@tinymce/tinymce-react';
import { Alert, Box } from '@mantine/core';

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
  const [apiKeyMissing, setApiKeyMissing] = useState(false);
  // Temporarily hardcoding API key since env variable not loading
  // TODO: Fix environment variable loading issue
  const apiKey = '3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp';
  // const apiKey = import.meta.env.VITE_TINYMCE_API_KEY;

  useEffect(() => {
    if (!apiKey) {
      setApiKeyMissing(true);
      console.warn('TinyMCE API key not configured. Please set VITE_TINYMCE_API_KEY in your .env file.');
    }
  }, [apiKey]);

  if (!apiKey) {
    return (
      <Box>
        <Alert color="orange" mb="xs" title="Configuration Notice">
          TinyMCE API key not configured. Please set VITE_TINYMCE_API_KEY in your .env.development file.
          <br />
          Current environment: {import.meta.env.MODE}
        </Alert>
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