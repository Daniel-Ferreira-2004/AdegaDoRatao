import axios, { AxiosError } from 'axios'
import { env } from '@/config/env'

const TOKEN_KEY = 'adega.session'

export interface StoredSession {
  accessToken: string
  expiresAt: string
  userId: string
  name: string
  role: string
  permissions: string[]
}

export function getStoredSession(): StoredSession | null {
  try {
    const raw = localStorage.getItem(TOKEN_KEY)
    if (!raw) return null
    const session = JSON.parse(raw) as StoredSession
    if (new Date(session.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(TOKEN_KEY)
      return null
    }
    return session
  } catch {
    localStorage.removeItem(TOKEN_KEY)
    return null
  }
}

export function storeSession(session: StoredSession) {
  localStorage.setItem(TOKEN_KEY, JSON.stringify(session))
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
}

/** Erro normalizado da API (ProblemDetails / ValidationProblemDetails). */
export class ApiError extends Error {
  status: number
  title: string
  fieldErrors: Record<string, string[]>

  constructor(status: number, title: string, detail: string, fieldErrors: Record<string, string[]> = {}) {
    super(detail || title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.fieldErrors = fieldErrors
  }
}

interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function normalizeApiError(error: unknown): ApiError {
  if (axios.isAxiosError(error)) {
    const err = error as AxiosError<ProblemDetails>
    if (!err.response) {
      return new ApiError(0, 'Sem conexão', 'Não foi possível conectar ao servidor. Verifique se a API está em execução.')
    }
    const { status, data } = err.response
    const title = data?.title ?? defaultTitle(status)
    const detail = data?.detail ?? 'Ocorreu um erro inesperado.'
    return new ApiError(status, title, detail, data?.errors ?? {})
  }
  return new ApiError(0, 'Erro inesperado', error instanceof Error ? error.message : 'Erro desconhecido.')
}

function defaultTitle(status: number): string {
  switch (status) {
    case 400: return 'Requisição inválida'
    case 401: return 'Sessão expirada'
    case 403: return 'Acesso negado'
    case 404: return 'Registro não encontrado'
    case 409: return 'Conflito'
    default: return 'Erro no servidor'
  }
}

export const apiClient = axios.create({
  baseURL: env.apiUrl,
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config) => {
  const session = getStoredSession()
  if (session) {
    config.headers.Authorization = `Bearer ${session.accessToken}`
  }
  return config
})

type UnauthorizedListener = () => void
let onUnauthorized: UnauthorizedListener | null = null
export function setUnauthorizedHandler(listener: UnauthorizedListener) {
  onUnauthorized = listener
}

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const normalized = normalizeApiError(error)
    if (normalized.status === 401 && onUnauthorized) {
      onUnauthorized()
    }
    return Promise.reject(normalized)
  },
)
