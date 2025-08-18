// Mock API functions for form component testing

/**
 * Mock email uniqueness check
 * Returns false for "taken@example.com" to simulate existing email
 * Simulates 1 second delay
 */
export const mockCheckEmailUnique = async (email: string): Promise<boolean> => {
  // Simulate API delay
  await new Promise(resolve => setTimeout(resolve, 1000));
  
  // Return false for testing existing email scenario
  return email.toLowerCase() !== 'taken@example.com';
};

/**
 * Mock scene name uniqueness check
 * Returns false for "admin" to simulate existing scene name
 * Simulates 1 second delay
 */
export const mockCheckSceneNameUnique = async (sceneName: string): Promise<boolean> => {
  // Simulate API delay
  await new Promise(resolve => setTimeout(resolve, 1000));
  
  // Return false for testing existing scene name scenario
  return sceneName.toLowerCase() !== 'admin';
};

/**
 * Mock form submission
 * Simulates successful form submission with delay
 */
export const mockSubmitForm = async (data: any): Promise<void> => {
  console.log('Form submitted with data:', data);
  
  // Simulate processing delay
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  // Could throw error to test error handling
  // throw new Error('Submission failed for testing');
};