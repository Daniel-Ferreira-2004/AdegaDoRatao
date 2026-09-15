import { apiClient } from '@/services/api/apiClient'
import type { DashboardResponse } from '@/types/api'

export async function getDashboard(): Promise<DashboardResponse> {
  const { data } = await apiClient.get<DashboardResponse>('/dashboard')
  return data
}
