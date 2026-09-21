import { apiClient } from '@/services/api/apiClient'
import type { LoginRequest, LoginResponse } from '@/types/api'

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>('/auth/login', request)
  return data
}

/** Revoga o refresh token no servidor (logout). Falhas são ignoradas — o logout local acontece de qualquer forma. */
export async function revoke(refreshToken: string): Promise<void> {
  try {
    await apiClient.post('/auth/revoke', { refreshToken })
  } catch {
    // melhor esforço: mesmo sem rede, a sessão local é encerrada
  }
}
