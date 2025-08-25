import React from 'react';
import { Textarea } from '@mantine/core';

interface TinyMCERichTextEditorProps {
  value?: string;
  onChange?: (content: string) => void;
  placeholder?: string;
  minRows?: number;
}

/**
 * Temporary simple implementation to replace TinyMCE
 * This prevents the import error while we focus on wireframe testing
 * NO TINYMCE IMPORTS - just a simple textarea wrapper
 */
export const TinyMCERichTextEditor: React.FC<TinyMCERichTextEditorProps> = ({
  value = '',
  onChange,
  placeholder = 'Enter text...',
  minRows = 4
}) => {
  return (
    <Textarea
      value={value}
      onChange={(event) => onChange?.(event.currentTarget.value)}
      placeholder={placeholder}
      minRows={minRows}
      autosize
    />
  );
};