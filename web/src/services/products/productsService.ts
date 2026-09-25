import { apiClient } from '@/services/api/apiClient'
import type {
  ActiveStatusRequest,
  ChangeProductPricesRequest,
  CreateProductRequest,
  PagedResult,
  PrecoMercadoResponse,
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

export async function getPrecosMercado(id: string): Promise<PrecoMercadoResponse> {
  const { data } = await apiClient.get<PrecoMercadoResponse>(`/products/${id}/precos-mercado`)
  return data
}

/** Dispara a coleta de preços de um EAN em todas as redes (agente de preços). */
export async function atualizarPrecosMercado(ean: string): Promise<void> {
  // Sem timeout: os coletores via navegador (Shibata, Sonda, D'avó) são
  // lentos (15-45s cada) e o padrão de 30s do apiClient abortaria a
  // requisição antes de terminarem.
  await apiClient.post(`/precos/atualizar/${encodeURIComponent(ean)}`, undefined, { timeout: 0 })
}

export async function deleteProduct(id: string): Promise<void> {
  await apiClient.delete(`/products/${id}`)
}
