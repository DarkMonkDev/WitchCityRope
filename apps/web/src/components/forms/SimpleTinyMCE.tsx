import React from 'react';
import { Editor } from '@tinymce/tinymce-react';

export default function SimpleTinyMCE() {
  return (
    <div>
      <h3>Simple TinyMCE Test</h3>
      <Editor
        apiKey='3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp'
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