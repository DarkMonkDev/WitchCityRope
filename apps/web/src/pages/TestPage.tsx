import React from 'react';

export const TestPage: React.FC = () => {
  return (
    <div style={{ padding: '20px', background: '#f0f0f0' }}>
      <h1 style={{ color: '#880124' }}>React Test Page</h1>
      <p>If you can see this, React is working!</p>
      <button onClick={() => alert('Button clicked!')}>Test Button</button>
    </div>
  );
};