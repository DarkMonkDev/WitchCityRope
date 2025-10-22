import { apiClient } from '../../../../lib/api/client';
import type { EmailTemplateResponse, UpdateEmailTemplateRequest } from '../types/emailTemplates.types';

/**
 * API service for managing vetting email templates
 * Handles GET and PUT requests to email template endpoints
 */
export class EmailTemplatesApiService {
  /**
   * Get all active email templates
   * GET /api/vetting/email-templates
   */
  async getEmailTemplates(): Promise<EmailTemplateResponse[]> {
    try {
      console.log('EmailTemplatesApi.getEmailTemplates: Fetching all templates');

      const response = await apiClient.get<{ success: boolean; data: EmailTemplateResponse[] }>(
        '/api/vetting/email-templates'
      );

      console.log('EmailTemplatesApi.getEmailTemplates: Response received', {
        success: response.data.success,
        templateCount: response.data.data?.length || 0
      });

      if (!response.data.success) {
        throw new Error('API returned success: false when fetching email templates');
      }

      return response.data.data || [];
    } catch (error: any) {
      console.error('EmailTemplatesApi.getEmailTemplates error:', {
        error: error.message || error,
        status: error.response?.status,
        statusText: error.response?.statusText
      });

      // Enhance error message based on HTTP status
      if (error.response?.status === 401) {
        throw new Error('Authentication required. Please log in to view email templates.');
      } else if (error.response?.status === 403) {
        throw new Error('Access denied. Administrator role required to view email templates.');
      } else if (error.response?.status >= 500) {
        throw new Error('Server error occurred while loading email templates. Please try again later.');
      } else if (error.message?.includes('Network')) {
        throw new Error('Network error. Please check your internet connection and try again.');
      }

      // Re-throw original error if no specific handling applies
      throw error;
    }
  }

  /**
   * Update an email template
   * PUT /api/vetting/email-templates/{id}
   */
  async updateEmailTemplate(
    id: string,
    data: UpdateEmailTemplateRequest
  ): Promise<EmailTemplateResponse> {
    try {
      console.log('EmailTemplatesApi.updateEmailTemplate: Updating template', {
        id,
        subjectLength: data.subject?.length || 0,
        htmlBodyLength: data.htmlBody?.length || 0
      });

      const response = await apiClient.put<{ success: boolean; data: EmailTemplateResponse }>(
        `/api/vetting/email-templates/${id}`,
        data
      );

      console.log('EmailTemplatesApi.updateEmailTemplate: Update successful', {
        templateId: response.data.data?.id,
        version: response.data.data?.version
      });

      if (!response.data.success) {
        throw new Error('API returned success: false when updating email template');
      }

      if (!response.data.data) {
        throw new Error('No template data received after update');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesApi.updateEmailTemplate error:', {
        id,
        error: error.message || error,
        status: error.response?.status,
        statusText: error.response?.statusText
      });

      // Enhance error message based on HTTP status
      if (error.response?.status === 400) {
        const validationMessage = error.response?.data?.message || 'Invalid template data';
        throw new Error(`Validation error: ${validationMessage}`);
      } else if (error.response?.status === 401) {
        throw new Error('Authentication required. Please log in to update email templates.');
      } else if (error.response?.status === 403) {
        throw new Error('Access denied. Administrator role required to update email templates.');
      } else if (error.response?.status === 404) {
        throw new Error(`Email template with ID "${id}" was not found.`);
      } else if (error.response?.status >= 500) {
        throw new Error('Server error occurred while updating email template. Please try again later.');
      } else if (error.message?.includes('Network')) {
        throw new Error('Network error. Please check your internet connection and try again.');
      }

      // Re-throw original error if no specific handling applies
      throw error;
    }
  }
}

export const emailTemplatesApi = new EmailTemplatesApiService();
