import { apiClient } from '@/services/api/apiClient'
import type {
  ActiveStatusRequest,
  ChangeProductPricesRequest,
  CreateProductRequest,
  PagedResult,
  ProductQuery,
  ProductResponse,
  UpdateProductRequest,
} from '@/types/api'

export async function getProducts(query: ProductQuery): Promise<PagedResult<ProductResponse>> {
  const { data } = await apiClient.get<PagedResult<ProductResponse>>('/products', { params: query })
  return data
}

export async function getProduct(id: string): Promise<ProductResponse> {
  const { data } = await apiClient.get<ProductResponse>(`/products/${id}`)
  return data
}

export async function createProduct(request: CreateProductRequest): Promise<ProductResponse> {
  const { data } = await apiClient.post<ProductResponse>('/products', request)
  return data
}

export async function updateProduct(id: string, request: UpdateProductRequest): Promise<ProductResponse> {
  const { data } = await apiClient.put<ProductResponse>(`/products/${id}`, request)
  return data
}

export async function changeProductPrices(id: string, request: ChangeProductPricesRequest): Promise<ProductResponse> {
  const { data } = await apiClient.patch<ProductResponse>(`/products/${id}/prices`, request)
  return data
}

export async function setProductActive(id: string, request: ActiveStatusRequest): Promise<void> {
  await apiClient.patch(`/products/${id}/active`, request)
}
