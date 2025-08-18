import { forwardRef, useState, useEffect, useCallback } from 'react';
import { BaseInput, BaseInputProps } from './BaseInput';
import { useDebouncedValue } from '@mantine/hooks';

export interface SceneNameInputProps extends BaseInputProps {
  checkUniqueness?: boolean;
  debounceMs?: number;
  excludeCurrentUser?: boolean;
  onUniquenessCheck?: (sceneName: string) => Promise<boolean>;
}

// Scene name validation regex (from business requirements)
const SCENE_NAME_REGEX = /^[a-zA-Z0-9_-]+$/;

export const SceneNameInput = forwardRef<HTMLInputElement, SceneNameInputProps>(
  ({ 
    checkUniqueness = false,
    debounceMs = 500,
    excludeCurrentUser = false,
    onUniquenessCheck,
    value = '',
    error,
    'data-testid': testId = 'scene-name-input',
    description = "This is how you'll be known in the community",
    ...props 
  }, ref) => {
    const [isValidatingUniqueness, setIsValidatingUniqueness] = useState(false);
    const [uniquenessError, setUniquenessError] = useState<string>('');
    
    // Debounce scene name value for async validation
    const [debouncedSceneName] = useDebouncedValue(String(value), debounceMs);

    // Check scene name uniqueness
    const checkSceneNameUniqueness = useCallback(async (sceneName: string) => {
      if (!checkUniqueness || !onUniquenessCheck || !sceneName || sceneName.length < 2) {
        return;
      }

      // Only check if it passes basic validation
      if (!SCENE_NAME_REGEX.test(sceneName)) {
        return;
      }

      setIsValidatingUniqueness(true);
      setUniquenessError('');

      try {
        const isUnique = await onUniquenessCheck(sceneName);
        if (!isUnique) {
          setUniquenessError('This scene name is already taken');
        }
      } catch (error) {
        // Silently fail on network errors - don't block user
        console.warn('Scene name uniqueness check failed:', error);
      } finally {
        setIsValidatingUniqueness(false);
      }
    }, [checkUniqueness, onUniquenessCheck]);

    // Trigger uniqueness check when debounced scene name changes
    useEffect(() => {
      if (debouncedSceneName && debouncedSceneName !== value) {
        // Only check if the debounced value is different from current value
        return;
      }
      checkSceneNameUniqueness(debouncedSceneName);
    }, [debouncedSceneName, checkSceneNameUniqueness, value]);

    // Combine validation errors
    const finalError = error || uniquenessError;

    return (
      <BaseInput
        ref={ref}
        type="text"
        label="Scene name"
        placeholder="YourSceneName"
        description={description}
        value={value}
        error={finalError}
        loading={isValidatingUniqueness}
        data-testid={testId}
        autoComplete="username"
        minLength={2}
        maxLength={50}
        {...props}
      />
    );
  }
);

SceneNameInput.displayName = 'SceneNameInput';