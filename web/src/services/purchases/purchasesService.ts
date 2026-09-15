import { apiClient } from '@/services/api/apiClient'
import type { CreatePurchaseRequest, PurchaseResponse } from '@/types/api'

export async function createPurchase(request: CreatePurchaseRequest): Promise<PurchaseResponse> {
  const { data } = await apiClient.post<PurchaseResponse>('/purchases', request)
  return data
}

export async function confirmPurchase(id: string): Promise<void> {
  await apiClient.post(`/purchases/${id}/confirm`)
}

export async function cancelPurchase(id: string): Promise<void> {
  await apiClient.post(`/purchases/${id}/cancel`)
}
