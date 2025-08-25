import React, { useEffect, useState } from 'react';
import { Editor } from '@tinymce/tinymce-react';
import { Text, Box, Alert } from '@mantine/core';

interface TinyMCERichTextEditorProps {
  value: string;
  onChange: (content: string) => void;
  label?: string;
  description?: string;
  required?: boolean;
  error?: string;
  height?: number;
  placeholder?: string;
}

/**
 * TinyMCE Rich Text Editor Component
 * 
 * Mandatory rich text editor for all admin content management.
 * Uses TinyMCE as per UI Implementation Standards.
 * 
 * Reference: /docs/standards-processes/ui-implementation-standards.md
 */
export const TinyMCERichTextEditor: React.FC<TinyMCERichTextEditorProps> = ({
  value,
  onChange,
  label,
  description,
  required = false,
  error,
  height = 300,
  placeholder = 'Start typing...'
}) => {
  const [apiKeyMissing, setApiKeyMissing] = useState(false);
  const apiKey = import.meta.env.VITE_TINYMCE_API_KEY;

  useEffect(() => {
    if (!apiKey) {
      console.warn('TinyMCE API key not found. Some features may be limited.');
      setApiKeyMissing(true);
    }
  }, [apiKey]);
  return (
    <Box mb="md">
      {label && (
        <Text size="sm" fw={500} mb={5}>
          {label} {required && <Text component="span" c="red">*</Text>}
        </Text>
      )}
      {description && (
        <Text size="xs" c="dimmed" mb="xs">
          {description}
        </Text>
      )}
      
      {apiKeyMissing && (
        <Alert color="orange" mb="xs" title="Configuration Notice">
          TinyMCE API key not configured. Using basic functionality. Set VITE_TINYMCE_API_KEY in your .env file.
        </Alert>
      )}
      
      <Editor
        value={value}
        onEditorChange={onChange}
        init={{
          height,
          menubar: false,
          plugins: [
            'advlist', 'autolink', 'lists', 'link', 'image', 'charmap',
            'anchor', 'searchreplace', 'visualblocks', 'code', 'fullscreen',
            'insertdatetime', 'media', 'table', 'help', 'wordcount'
          ],
          toolbar: 'undo redo | blocks | ' +
            'bold italic forecolor | alignleft aligncenter ' +
            'alignright alignjustify | bullist numlist outdent indent | ' +
            'removeformat | help',
          content_style: `
            body { 
              font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
              font-size: 14px;
              line-height: 1.6;
              color: #333;
              max-width: none;
              margin: 8px;
            }
            p { margin: 0 0 8px 0; }
            h1, h2, h3, h4, h5, h6 { 
              color: var(--mantine-color-burgundy-6, #880124); 
              margin: 12px 0 8px 0;
            }
            a { color: var(--mantine-color-burgundy-6, #880124); }
          `,
          placeholder,
          branding: false,
          statusbar: false,
          resize: false,
          // Basic configuration - no premium features needed
          skin: 'oxide',
          content_css: 'default',
          // Ensure clean HTML output
          valid_elements: 'p,br,strong,em,u,s,a[href],h1,h2,h3,h4,h5,h6,ul,ol,li,blockquote,code',
          // Remove unwanted elements
          invalid_elements: 'script,object,embed,iframe',
          // Clean up on paste
          paste_as_text: false,
          paste_block_drop: true,
          paste_data_images: false
        }}
        apiKey={apiKey}
      />
      
      {error && (
        <Text size="xs" c="red" mt={5}>
          {error}
        </Text>
      )}
    </Box>
  );
};