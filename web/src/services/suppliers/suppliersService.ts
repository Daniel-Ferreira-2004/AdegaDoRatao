import { apiClient } from '@/services/api/apiClient'
import type { ActiveStatusRequest, SupplierRequest, SupplierResponse } from '@/types/api'

export async function getSuppliers(): Promise<SupplierResponse[]> {
  const { data } = await apiClient.get<SupplierResponse[]>('/suppliers')
  return data
}

export async function createSupplier(request: SupplierRequest): Promise<SupplierResponse> {
  const { data } = await apiClient.post<SupplierResponse>('/suppliers', request)
  return data
}

export async function updateSupplier(id: string, request: SupplierRequest): Promise<SupplierResponse> {
  const { data } = await apiClient.put<SupplierResponse>(`/suppliers/${id}`, request)
  return data
}

export async function setSupplierActive(id: string, request: ActiveStatusRequest): Promise<void> {
  await apiClient.patch(`/suppliers/${id}/active`, request)
}
