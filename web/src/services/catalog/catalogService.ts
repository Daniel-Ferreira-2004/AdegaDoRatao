import { apiClient } from '@/services/api/apiClient'
import type { ActiveStatusRequest, NamedEntityRequest, NamedEntityResponse } from '@/types/api'

async function list(resource: string): Promise<NamedEntityResponse[]> {
  const { data } = await apiClient.get<NamedEntityResponse[]>(`/${resource}`)
  return data
}

async function create(resource: string, request: NamedEntityRequest): Promise<NamedEntityResponse> {
  const { data } = await apiClient.post<NamedEntityResponse>(`/${resource}`, request)
  return data
}

async function setActive(resource: string, id: string, request: ActiveStatusRequest): Promise<void> {
  await apiClient.patch(`/${resource}/${id}/active`, request)
}

export const categoriesService = {
  list: () => list('categories'),
  create: (request: NamedEntityRequest) => create('categories', request),
  setActive: (id: string, request: ActiveStatusRequest) => setActive('categories', id, request),
}

export const brandsService = {
  list: () => list('brands'),
  create: (request: NamedEntityRequest) => create('brands', request),
  setActive: (id: string, request: ActiveStatusRequest) => setActive('brands', id, request),
}

export const paymentMethodsService = {
  list: () => list('payment-methods'),
  create: (request: NamedEntityRequest) => create('payment-methods', request),
  setActive: (id: string, request: ActiveStatusRequest) => setActive('payment-methods', id, request),
}
