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
 * User Segment DTO
 * Source: C# UserSegmentDto via NSwag generation
 */
export type UserSegmentDto = components['schemas']['UserSegmentDto'];

/**
 * User Preview DTO
 * Source: C# UserPreviewDto via NSwag generation
 */
export type UserPreviewDto = components['schemas']['UserPreviewDto'];

/**
 * User Segment Enum
 * Source: C# UserSegment enum via NSwag generation
 */
export type UserSegment = components['schemas']['UserSegment'];

/**
 * Trigger Configuration DTO
 * TODO: Replace with auto-generated type when backend DTOs are added
 * Source: C# TriggerConfigDto via NSwag generation
 */
export type TriggerConfigDto = {
  triggerType: 'FixedEvent' | 'TimeBased' | 'Manual';
  triggerEnabled: boolean;
  timingOffsetDays?: number;
  recipientGroup?: string;
};

/**
 * Update Trigger Config Request
 * TODO: Replace with auto-generated type when backend DTOs are added
 * Source: C# UpdateTriggerConfigRequest via NSwag generation
 */
export type UpdateTriggerConfigRequest = {
  triggerType: 'FixedEvent' | 'TimeBased' | 'Manual';
  triggerEnabled: boolean;
  timingOffsetDays?: number;
  recipientGroup?: string;
};

/**
 * Ad Hoc Email Template DTO
 * TODO: Replace with auto-generated type when backend DTOs are added
 * Source: C# AdHocEmailTemplateDto via NSwag generation
 */
export type AdHocEmailTemplateDto = {
  id: string;
  templateName: string;
  subject: string;
  htmlBody: string;
  plainTextBody: string;
  createdAt: string;
  createdBy: string;
};

/**
 * Save As Template Request
 * TODO: Replace with auto-generated type when backend DTOs are added
 * Source: C# SaveAsTemplateRequest via NSwag generation
 */
export type SaveAsTemplateRequest = {
  templateName: string;
  subject: string;
  htmlBody: string;
  plainTextBody: string;
};

/**
 * Schedule Ad Hoc Email Request
 * TODO: Replace with auto-generated type when backend DTOs are added
 * Source: C# ScheduleAdHocEmailRequest via NSwag generation
 */
export type ScheduleAdHocEmailRequest = SendAdHocEmailRequest & {
  scheduledSendAt?: string;
};

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
      const response = await apiClient.get<GlobalEmailTemplateDto[]>(
        `/api/email-templates?category=${category}`
      );

      console.log('EmailTemplatesAPI: Global templates response:', {
        hasData: !!response.data,
        count: response.data?.length || 0,
      });

      return response.data;
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
      const response = await apiClient.get<GlobalEmailTemplateDto>(
        `/api/email-templates/${id}`
      );

      return response.data;
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
      const response = await apiClient.put<GlobalEmailTemplateDto>(
        `/api/email-templates/${id}`,
        request
      );

      return response.data;
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
      const response = await apiClient.get<EventEmailTemplateDto[]>(
        `/api/email-templates/events/${eventId}`
      );

      console.log('EmailTemplatesAPI: Event templates response:', {
        hasData: !!response.data,
        count: response.data?.length || 0,
      });

      return response.data;
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
      const response = await apiClient.get<EventEmailTemplateDto>(
        `/api/email-templates/events/${eventId}/${templateType}`
      );

      return response.data;
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
      const response = await apiClient.put<EventEmailTemplateDto>(
        `/api/email-templates/events/${eventId}/${templateType}`,
        request
      );

      return response.data;
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
   * Get all user segments with recipient counts
   */
  async getUserSegments(): Promise<UserSegmentDto[]> {
    console.log('EmailTemplatesAPI: Fetching user segments');

    try {
      const response = await apiClient.get<UserSegmentDto[]>(
        `/api/email-templates/segments`
      );

      console.log('EmailTemplatesAPI: User segments response:', {
        hasData: !!response.data,
        count: response.data?.length || 0,
      });

      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching user segments:', {
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Get preview of first 10 users from a segment
   */
  async getSegmentPreview(segmentName: string): Promise<UserPreviewDto[]> {
    console.log('EmailTemplatesAPI: Fetching segment preview:', segmentName);

    try {
      const response = await apiClient.get<UserPreviewDto[]>(
        `/api/email-templates/segments/${segmentName}/preview`
      );

      console.log('EmailTemplatesAPI: Segment preview response:', {
        hasData: !!response.data,
        count: response.data?.length || 0,
      });

      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching segment preview:', {
        segmentName,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Send ad-hoc bulk email
   */
  async sendAdHocEmail(request: SendAdHocEmailRequest): Promise<SentAdHocEmailDto> {
    console.log('EmailTemplatesAPI: Sending ad-hoc email:', request);

    try {
      const response = await apiClient.post<SentAdHocEmailDto>(
        `/api/email-templates/ad-hoc/send`,
        request
      );
      console.log('EmailTemplatesAPI: Ad-hoc email sent successfully');
      return response.data;
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

      const response = await apiClient.get<SentAdHocEmailDto[]>(url);

      return response.data;
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
      const response = await apiClient.get<SentAdHocEmailDto>(
        `/api/email-templates/ad-hoc/history/${id}`
      );

      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching ad-hoc email:', {
        id,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  // ===================================================================
  // TRIGGER ENHANCEMENTS (Events tab - time-based triggers)
  // ===================================================================

  /**
   * Update trigger configuration for an event template
   */
  async updateTriggerConfig(
    id: string,
    request: UpdateTriggerConfigRequest
  ): Promise<GlobalEmailTemplateDto> {
    console.log('EmailTemplatesAPI: Updating trigger config:', { id, request });

    try {
      const response = await apiClient.put<GlobalEmailTemplateDto>(
        `/api/email-templates/${id}/trigger-config`,
        request
      );

      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error updating trigger config:', {
        id,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Get all time-based templates for Events category
   */
  async getTimeBasedTemplates(): Promise<GlobalEmailTemplateDto[]> {
    console.log('EmailTemplatesAPI: Fetching time-based templates');

    try {
      const response = await apiClient.get<GlobalEmailTemplateDto[]>(
        '/api/email-templates/time-based'
      );

      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching time-based templates:', {
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  // ===================================================================
  // AD HOC TEMPLATE ENHANCEMENTS (Save/Delete templates, Scheduled send)
  // ===================================================================

  /**
   * Get all saved ad hoc templates
   */
  async getAdHocTemplates(): Promise<AdHocEmailTemplateDto[]> {
    console.log('EmailTemplatesAPI: Fetching ad hoc templates');

    try {
      const response = await apiClient.get<AdHocEmailTemplateDto[]>(
        '/api/email-templates/ad-hoc/templates'
      );

      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching ad hoc templates:', {
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Save an ad hoc email as a reusable template
   */
  async saveAsTemplate(request: SaveAsTemplateRequest): Promise<AdHocEmailTemplateDto> {
    console.log('EmailTemplatesAPI: Saving ad hoc template:', request);

    try {
      const response = await apiClient.post<AdHocEmailTemplateDto>(
        '/api/email-templates/ad-hoc/templates',
        request
      );

      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error saving ad hoc template:', {
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Delete a saved ad hoc template
   */
  async deleteAdHocTemplate(id: string): Promise<void> {
    console.log('EmailTemplatesAPI: Deleting ad hoc template:', id);

    try {
      await apiClient.delete(`/api/email-templates/ad-hoc/templates/${id}`);
      console.log('EmailTemplatesAPI: Ad hoc template deleted successfully');
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error deleting ad hoc template:', {
        id,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Schedule an ad hoc email for future delivery
   */
  async scheduleAdHocEmail(request: ScheduleAdHocEmailRequest): Promise<SentAdHocEmailDto> {
    console.log('EmailTemplatesAPI: Scheduling ad hoc email:', request);

    try {
      const response = await apiClient.post<SentAdHocEmailDto>(
        '/api/email-templates/ad-hoc/schedule',
        request
      );

      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error scheduling ad hoc email:', {
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }


  // ===================================================================
  // EMAIL TEMPLATE TESTING (Admin-only)
  // ===================================================================

  /**
   * Fetch all saved test data variable values for email template testing
   */
  async getTestData(): Promise<Record<string, string>> {
    console.log('EmailTemplatesAPI: Fetching email test data');
    try {
      const response = await apiClient.get<Record<string, string>>(
        '/api/email-templates/test-data'
      );
      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error fetching test data:', {
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Save test data variable values (upsert — creates new entries if missing, updates existing)
   */
  async saveTestData(testData: Record<string, string>): Promise<void> {
    console.log('EmailTemplatesAPI: Saving email test data');
    try {
      await apiClient.put('/api/email-templates/test-data', testData);
      console.log('EmailTemplatesAPI: Test data saved successfully');
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error saving test data:', {
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }

  /**
   * Send a test email for a specific template with variable substitution
   * @param templateId - Global email template ID
   * @param request - Email address and optional variable overrides
   */
  async sendTestEmail(
    templateId: string,
    request: { email: string; variableOverrides?: Record<string, string> }
  ): Promise<{ message: string; templateType: string; sentTo: string }> {
    console.log('EmailTemplatesAPI: Sending test email:', { templateId, request });
    try {
      const response = await apiClient.post<{
        message: string;
        templateType: string;
        sentTo: string;
      }>(`/api/email-templates/${templateId}/send-test`, request);
      console.log('EmailTemplatesAPI: Test email sent successfully');
      return response.data;
    } catch (error: any) {
      console.error('EmailTemplatesAPI: Error sending test email:', {
        templateId,
        error: error.message || error,
        status: error.response?.status,
      });
      throw error;
    }
  }
}

// Export singleton instance
export const emailTemplatesApi = new EmailTemplatesApiService();
