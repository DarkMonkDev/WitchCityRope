import { apiClient } from '../lib/api/client';
import type { components } from '@witchcityrope/shared-types';

// =================================================================
// TYPE DEFINITIONS - AUTO-GENERATED FROM BACKEND
// =================================================================

/**
 * Global Email Template DTO
 * Source: C# GlobalEmailTemplateDto via NSwag generation
 */
export type GlobalEmailTemplateDto = components['schemas']['GlobalEmailTemplateDto'];

/**
 * Event Email Template DTO
 * Source: C# EventEmailTemplateDto via NSwag generation
 */
export type EventEmailTemplateDto = components['schemas']['EventEmailTemplateDto'];

/**
 * Sent Ad Hoc Email DTO
 * Source: C# SentAdHocEmailDto via NSwag generation
 */
export type SentAdHocEmailDto = components['schemas']['SentAdHocEmailDto'];

/**
 * Update Global Template Request
 * Source: C# UpdateGlobalTemplateRequest via NSwag generation
 */
export type UpdateGlobalTemplateRequest = components['schemas']['UpdateGlobalTemplateRequest'];

/**
 * Update Event Template Request
 * Source: C# UpdateEventTemplateRequest via NSwag generation
 */
export type UpdateEventTemplateRequest = components['schemas']['UpdateEventTemplateRequest'];

/**
 * Send Ad Hoc Email Request
 * Source: C# SendAdHocEmailRequest via NSwag generation
 */
export type SendAdHocEmailRequest = components['schemas']['SendAdHocEmailRequest'];

/**
 * API Response Wrapper
 */
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  timestamp?: string;
}

// =================================================================
// EMAIL TEMPLATES API SERVICE
// =================================================================

class EmailTemplatesApiService {
  // ===================================================================
  // GLOBAL TEMPLATES (Admin-only)
  // ===================================================================

  /**
   * Get all global email templates for a specific category
   * @param category - 'Vetting' | 'Events' | 'Admin' | 'Incident' | 'AdHoc'
   */
  async getGlobalTemplatesByCategory(
    category: string
  ): Promise<GlobalEmailTemplateDto[]> {
    console.log('EmailTemplatesAPI: Fetching global templates for category:', category);

    try {
      const response = await apiClient.get<ApiResponse<GlobalEmailTemplateDto[]>>(
        `/api/email-templates?category=${category}`
      );

      console.log('EmailTemplatesAPI: Global templates response:', {
        hasData: !!response.data,
        success: response.data.success,
        count: response.data.data?.length || 0,
      });

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to fetch global templates');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching global templates:', {
        category,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Get a single global email template by ID
   */
  async getGlobalTemplateById(id: string): Promise<GlobalEmailTemplateDto> {
    console.log('EmailTemplatesAPI: Fetching global template:', id);

    try {
      const response = await apiClient.get<ApiResponse<GlobalEmailTemplateDto>>(
        `/api/email-templates/${id}`
      );

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to fetch global template');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching global template:', {
        id,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Update a global email template
   */
  async updateGlobalTemplate(
    id: string,
    request: UpdateGlobalTemplateRequest
  ): Promise<GlobalEmailTemplateDto> {
    console.log('EmailTemplatesAPI: Updating global template:', { id, request });

    try {
      const response = await apiClient.put<ApiResponse<GlobalEmailTemplateDto>>(
        `/api/email-templates/${id}`,
        request
      );

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to update global template');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error updating global template:', {
        id,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  // ===================================================================
  // EVENT TEMPLATES (Authorized users)
  // ===================================================================

  /**
   * Get all email templates for an event (merged global + overrides)
   */
  async getEventTemplates(eventId: string): Promise<EventEmailTemplateDto[]> {
    console.log('EmailTemplatesAPI: Fetching event templates:', eventId);

    try {
      const response = await apiClient.get<ApiResponse<EventEmailTemplateDto[]>>(
        `/api/email-templates/events/${eventId}`
      );

      console.log('EmailTemplatesAPI: Event templates response:', {
        hasData: !!response.data,
        success: response.data.success,
        count: response.data.data?.length || 0,
      });

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to fetch event templates');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching event templates:', {
        eventId,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Get a specific email template for an event by type
   */
  async getEventTemplateByType(
    eventId: string,
    templateType: string
  ): Promise<EventEmailTemplateDto> {
    console.log('EmailTemplatesAPI: Fetching event template:', { eventId, templateType });

    try {
      const response = await apiClient.get<ApiResponse<EventEmailTemplateDto>>(
        `/api/email-templates/events/${eventId}/${templateType}`
      );

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to fetch event template');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching event template:', {
        eventId,
        templateType,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Create or update an event-specific email template override
   * (Copy-on-edit pattern: creates new override on first save)
   */
  async updateEventTemplate(
    eventId: string,
    templateType: string,
    request: UpdateEventTemplateRequest
  ): Promise<EventEmailTemplateDto> {
    console.log('EmailTemplatesAPI: Updating event template:', {
      eventId,
      templateType,
      request,
    });

    try {
      const response = await apiClient.put<ApiResponse<EventEmailTemplateDto>>(
        `/api/email-templates/events/${eventId}/${templateType}`,
        request
      );

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to update event template');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error updating event template:', {
        eventId,
        templateType,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Delete an event-specific template override (reset to default)
   */
  async deleteEventTemplate(eventId: string, templateType: string): Promise<void> {
    console.log('EmailTemplatesAPI: Deleting event template override:', {
      eventId,
      templateType,
    });

    try {
      await apiClient.delete(`/api/email-templates/events/${eventId}/${templateType}`);
      console.log('EmailTemplatesAPI: Event template override deleted successfully');
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error deleting event template:', {
        eventId,
        templateType,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  // ===================================================================
  // AD HOC EMAILS (Admin-only)
  // ===================================================================

  /**
   * Send ad-hoc bulk email
   */
  async sendAdHocEmail(request: SendAdHocEmailRequest): Promise<void> {
    console.log('EmailTemplatesAPI: Sending ad-hoc email:', request);

    try {
      await apiClient.post(`/api/email-templates/ad-hoc/send`, request);
      console.log('EmailTemplatesAPI: Ad-hoc email sent successfully');
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error sending ad-hoc email:', {
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Get ad-hoc email send history
   * @param eventId - Optional filter by event ID
   */
  async getAdHocEmailHistory(eventId?: string): Promise<SentAdHocEmailDto[]> {
    console.log('EmailTemplatesAPI: Fetching ad-hoc email history:', { eventId });

    try {
      const url = eventId
        ? `/api/email-templates/ad-hoc/history?eventId=${eventId}`
        : `/api/email-templates/ad-hoc/history`;

      const response = await apiClient.get<ApiResponse<SentAdHocEmailDto[]>>(url);

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to fetch ad-hoc email history');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching ad-hoc email history:', {
        eventId,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Get a specific sent ad-hoc email by ID
   */
  async getAdHocEmailById(id: string): Promise<SentAdHocEmailDto> {
    console.log('EmailTemplatesAPI: Fetching ad-hoc email:', id);

    try {
      const response = await apiClient.get<ApiResponse<SentAdHocEmailDto>>(
        `/api/email-templates/ad-hoc/history/${id}`
      );

      if (!response.data.success || !response.data.data) {
        throw new Error(response.data.error || 'Failed to fetch ad-hoc email');
      }

      return response.data.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching ad-hoc email:', {
        id,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }
}

// Export singleton instance
export const emailTemplatesApi = new EmailTemplatesApiService();
