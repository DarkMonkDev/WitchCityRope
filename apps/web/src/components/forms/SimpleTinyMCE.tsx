import React from 'react';
import { Editor } from '@tinymce/tinymce-react';
import { Alert, Textarea } from '@mantine/core';

export default function SimpleTinyMCE() {
  const apiKey = import.meta.env.VITE_TINYMCE_API_KEY;

  if (!apiKey) {
    return (
      <div>
        <h3>Simple TinyMCE Test</h3>
        <Alert color="blue" mb="xs" title="Development Mode">
          TinyMCE disabled to prevent API usage costs. Using simple text editor.
        </Alert>
        <Textarea
          defaultValue="Welcome to the simple text editor!"
          minRows={15}
          autosize
          styles={{
            input: {
              fontFamily: '-apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica Neue, sans-serif',
              fontSize: '14px',
              lineHeight: '1.6'
            }
          }}
        />
      </div>
    );
  }

  return (
    <div>
      <h3>Simple TinyMCE Test</h3>
      <Editor
        apiKey={apiKey}
        init={{
          height: 300,
          menubar: false,
          plugins: [
            'anchor', 'autolink', 'charmap', 'codesample', 'emoticons', 'link', 'lists', 'media', 'searchreplace', 'table', 'visualblocks', 'wordcount'
          ],
          toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | link media table | align lineheight | numlist bullist indent outdent | emoticons charmap | removeformat',
        }}
        initialValue="<p>Welcome to TinyMCE!</p>"
      />
    </div>
  );
}