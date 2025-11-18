import { api } from '@/api/client';
import type { components } from '@witchcityrope/shared-types';

// ✅ Using auto-generated types from OpenAPI
type BackupJobResponse = components['schemas']['BackupJobResponse'];
type BackupListResponse = components['schemas']['BackupListResponse'];
type BackupJobStatusResponse = components['schemas']['BackupJobStatusResponse'];
type StorageSummaryResponse = components['schemas']['StorageSummaryResponse'];
type RestoreRequest = components['schemas']['RestoreRequest'];

export const backupApi = {
  triggerBackup: async (): Promise<BackupJobResponse> => {
    const { data } = await api.post<BackupJobResponse>('/api/admin/backup');
    return data;
  },

  listBackups: async (days?: number): Promise<BackupListResponse> => {
    const { data } = await api.get<BackupListResponse>('/api/admin/backup/list', {
      params: { days },
    });
    return data;
  },

  getJobStatus: async (jobId: string): Promise<BackupJobStatusResponse> => {
    const { data } = await api.get<BackupJobStatusResponse>(`/api/admin/backup/job/${jobId}`);
    return data;
  },

  getStorageSummary: async (): Promise<StorageSummaryResponse> => {
    const { data } = await api.get<StorageSummaryResponse>('/api/admin/backup/storage');
    return data;
  },

  deleteBackup: async (fileName: string): Promise<void> => {
    await api.delete(`/api/admin/backup/${fileName}`);
  },

  downloadBackup: async (fileName: string): Promise<Blob> => {
    const { data } = await api.get(`/api/admin/backup/download/${fileName}`, {
      responseType: 'blob',
    });
    return data;
  },

  restoreBackup: async (request: RestoreRequest): Promise<BackupJobResponse> => {
    const { data } = await api.post<BackupJobResponse>('/api/admin/backup/restore', request);
    return data;
  },

  uploadAndRestore: async (file: File, createPreBackup: boolean): Promise<BackupJobResponse> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('createPreBackup', createPreBackup.toString());

    const { data } = await api.post<BackupJobResponse>(
      '/api/admin/backup/upload-and-restore',
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
      }
    );
    return data;
  },
};
