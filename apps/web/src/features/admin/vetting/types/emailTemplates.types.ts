/**
 * Email template types for vetting system
 * These match the backend EmailTemplateResponse and UpdateEmailTemplateRequest DTOs
 */

/**
 * Email template response from API
 * Matches EmailTemplateResponse.cs in backend
 */
export interface EmailTemplateResponse {
  id: string;
  templateType: number;
  templateTypeName: string;
  subject: string;
  htmlBody: string;
  plainTextBody: string;
  variables: string;
  isActive: boolean;
  version: number;
  createdAt: string;
  updatedAt: string;
  lastModified: string;
  updatedBy: string;
  updatedByEmail: string;
}

/**
 * Request DTO for updating email template
 * Matches UpdateEmailTemplateRequest.cs in backend
 */
export interface UpdateEmailTemplateRequest {
  subject: string;
  htmlBody: string;
  plainTextBody: string;
}
