import { apiClient } from '@/services/api/apiClient'
import type { LoginRequest, LoginResponse } from '@/types/api'

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>('/auth/login', request)
  return data
}
