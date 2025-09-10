import React from 'react';

export const MinimalTest: React.FC = () => {
  console.log('MinimalTest component rendered');
  return (
    <div style={{ padding: '20px', fontSize: '24px', color: 'green' }}>
      <h1>React App is Working!</h1>
      <p>This is a minimal test to verify React is mounting correctly.</p>
    </div>
  );
};