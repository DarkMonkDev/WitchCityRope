import { useState, useEffect } from 'react';
import { emailTemplatesApi } from '../services/emailTemplates.api';

/**
 * Shared hook for fetching template variables from the code registry
 * and validating template content against them in real time.
 *
 * Used by both EmailCategoryPanel (global templates) and
 * EventEmailTemplatePanel (per-event templates) to ensure consistent
 * variable validation behavior.
 *
 * @param category - Email template category (e.g., 'Events', 'Vetting')
 * @param templateType - Template type within the category (e.g., 'RSVPConfirmation')
 * @param subject - Current subject line content to validate
 * @param htmlBody - Current HTML body content to validate
 * @returns availableVariables (from registry) and invalidVariables (found in content but not in registry)
 */
export function useTemplateVariableValidation(
  category: string | undefined,
  templateType: string | undefined,
  subject: string,
  htmlBody: string
) {
  const [availableVariables, setAvailableVariables] = useState<string[]>([]);
  const [invalidVariables, setInvalidVariables] = useState<string[]>([]);

  // Fetch available variables from the code registry when template selection changes
  useEffect(() => {
    if (!category || !templateType) {
      setAvailableVariables([]);
      return;
    }

    let cancelled = false;

    emailTemplatesApi
      .getVariablesForTemplate(category, templateType)
      .then((vars) => {
        if (!cancelled) {
          setAvailableVariables(vars);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [category, templateType]);

  // Real-time variable validation: extract {{variables}} from content
  // and compare against the code registry's allowed list
  useEffect(() => {
    if (!htmlBody) {
      setInvalidVariables([]);
      return;
    }

    const variablePattern = /\{\{([a-zA-Z0-9_]+)\}\}/g;
    const extractedVars = new Set<string>();

    // Extract from subject
    let match;
    while ((match = variablePattern.exec(subject)) !== null) {
      extractedVars.add(`{{${match[1]}}}`);
    }

    // Extract from HTML body (reset lastIndex since regex is stateful with /g flag)
    variablePattern.lastIndex = 0;
    while ((match = variablePattern.exec(htmlBody)) !== null) {
      extractedVars.add(`{{${match[1]}}}`);
    }

    // Only validate once variables have been fetched (non-empty array)
    // to avoid false warnings while the API request is in flight
    if (availableVariables.length === 0) {
      setInvalidVariables([]);
      return;
    }

    const invalid = Array.from(extractedVars).filter(
      (v) => !availableVariables.includes(v)
    );
    setInvalidVariables(invalid);
  }, [subject, htmlBody, availableVariables]);

  return { availableVariables, invalidVariables };
}
