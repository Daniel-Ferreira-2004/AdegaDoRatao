import { apiClient } from '@/services/api/apiClient'
import type {
  ProductResponse,
  RegisterStockMovementRequest,
  StockMovementResponse,
} from '@/types/api'

export async function registerMovement(request: RegisterStockMovementRequest): Promise<StockMovementResponse> {
  const { data } = await apiClient.post<StockMovementResponse>('/stock/movements', request)
  return data
}

export async function getProductMovements(
  productId: string,
  from?: string,
  to?: string,
): Promise<StockMovementResponse[]> {
  const { data } = await apiClient.get<StockMovementResponse[]>(`/stock/products/${productId}/movements`, {
    params: { from, to },
  })
  return data
}

export async function getLowStock(includeOutOfStock = true): Promise<ProductResponse[]> {
  const { data } = await apiClient.get<ProductResponse[]>('/stock/low', {
    params: { includeOutOfStock },
  })
  return data
}
