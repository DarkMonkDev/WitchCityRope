import React from 'react';
import SimpleTinyMCE from '../components/forms/SimpleTinyMCE';

export default function TestTinyMCE() {
  return (
    <div style={{ padding: '20px' }}>
      <h1>TinyMCE Test Page</h1>
      <p>This page tests TinyMCE with the API key hardcoded directly.</p>
      <SimpleTinyMCE />
    </div>
  );
}