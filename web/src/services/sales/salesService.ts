import { apiClient } from '@/services/api/apiClient'
import type {
  CreateSaleRequest,
  PagedResult,
  SaleListItemResponse,
  SaleQuery,
  SaleResponse,
} from '@/types/api'

export async function getSales(query: SaleQuery): Promise<PagedResult<SaleListItemResponse>> {
  const { data } = await apiClient.get<PagedResult<SaleListItemResponse>>('/sales', { params: query })
  return data
}

export async function getSale(id: string): Promise<SaleResponse> {
  const { data } = await apiClient.get<SaleResponse>(`/sales/${id}`)
  return data
}

export async function createSale(request: CreateSaleRequest): Promise<SaleResponse> {
  const { data } = await apiClient.post<SaleResponse>('/sales', request)
  return data
}

export async function cancelSale(id: string): Promise<void> {
  await apiClient.post(`/sales/${id}/cancel`)
}
