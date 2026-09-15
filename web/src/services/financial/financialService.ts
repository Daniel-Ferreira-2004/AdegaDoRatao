import { apiClient } from '@/services/api/apiClient'
import type {
  ActiveStatusRequest,
  CashFlowResponse,
  CreateFinancialCategoryRequest,
  CreateFinancialTransactionRequest,
  FinancialCategoryResponse,
  FinancialTransactionQuery,
  FinancialTransactionResponse,
  PagedResult,
} from '@/types/api'

export async function getFinancialCategories(): Promise<FinancialCategoryResponse[]> {
  const { data } = await apiClient.get<FinancialCategoryResponse[]>('/financial/categories')
  return data
}

export async function createFinancialCategory(
  request: CreateFinancialCategoryRequest,
): Promise<FinancialCategoryResponse> {
  const { data } = await apiClient.post<FinancialCategoryResponse>('/financial/categories', request)
  return data
}

export async function setFinancialCategoryActive(id: string, request: ActiveStatusRequest): Promise<void> {
  await apiClient.patch(`/financial/categories/${id}/active`, request)
}

export async function getTransactions(
  query: FinancialTransactionQuery,
): Promise<PagedResult<FinancialTransactionResponse>> {
  const { data } = await apiClient.get<PagedResult<FinancialTransactionResponse>>('/financial/transactions', {
    params: query,
  })
  return data
}

export async function createTransaction(
  request: CreateFinancialTransactionRequest,
): Promise<FinancialTransactionResponse> {
  const { data } = await apiClient.post<FinancialTransactionResponse>('/financial/transactions', request)
  return data
}

export async function reverseTransaction(id: string): Promise<void> {
  await apiClient.post(`/financial/transactions/${id}/estornar`)
}

export async function getCashFlow(from: string, to: string): Promise<CashFlowResponse> {
  const { data } = await apiClient.get<CashFlowResponse>('/financial/cashflow', { params: { from, to } })
  return data
}
