import { AxiosError, AxiosHeaders } from 'axios'
import { describe, expect, it } from 'vitest'
import { ApiError, normalizeApiError } from './apiClient'

function axiosError(status: number, data: unknown): AxiosError {
  const error = new AxiosError('Request failed')
  error.response = {
    status,
    data,
    statusText: '',
    headers: {},
    config: { headers: new AxiosHeaders() },
  }
  return error
}

describe('normalizeApiError', () => {
  it('extrai title e detail de ProblemDetails', () => {
    const error = normalizeApiError(
      axiosError(400, { status: 400, title: 'Regra de negócio violada', detail: 'Estoque insuficiente' }),
    )
    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(400)
    expect(error.title).toBe('Regra de negócio violada')
    expect(error.message).toBe('Estoque insuficiente')
  })

  it('extrai erros de validação por campo', () => {
    const error = normalizeApiError(
      axiosError(400, { status: 400, title: 'Validation', errors: { Name: ['O nome é obrigatório'] } }),
    )
    expect(error.fieldErrors.Name).toEqual(['O nome é obrigatório'])
  })

  it('retorna erro de conexão quando não há resposta', () => {
    const error = normalizeApiError(new AxiosError('Network Error'))
    expect(error.status).toBe(0)
    expect(error.title).toBe('Sem conexão')
  })

  it('usa título padrão por status quando não há body', () => {
    const error = normalizeApiError(axiosError(403, null))
    expect(error.title).toBe('Acesso negado')
  })
})
