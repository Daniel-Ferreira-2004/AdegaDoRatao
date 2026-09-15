import { apiClient } from '@/services/api/apiClient'
import type { AuditLogQuery, AuditLogResponse, PagedResult } from '@/types/api'

export async function getAuditLogs(query: AuditLogQuery): Promise<PagedResult<AuditLogResponse>> {
  const { data } = await apiClient.get<PagedResult<AuditLogResponse>>('/audit', { params: query })
  return data
}
