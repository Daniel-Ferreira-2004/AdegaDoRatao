import axios, { AxiosError } from 'axios'
import { env } from '@/config/env'

const TOKEN_KEY = 'adega.session'

export interface StoredSession {
  accessToken: string
  expiresAt: string
  refreshToken: string
  userId: string
  name: string
  role: string
  permissions: string[]
}

/** Lê a sessão do storage sem checar expiração (uso interno do refresh). */
function readRawSession(): StoredSession | null {
  try {
    const raw = localStorage.getItem(TOKEN_KEY)
    return raw ? (JSON.parse(raw) as StoredSession) : null
  } catch {
    localStorage.removeItem(TOKEN_KEY)
    return null
  }
}

export function getStoredSession(): StoredSession | null {
  const session = readRawSession()
  // JWT expirado não é sessão válida para a UI — mas o refresh token ainda
  // pode estar válido, então quem precisa dele usa readRawSession().
  if (!session || new Date(session.expiresAt).getTime() <= Date.now()) return null
  return session
}

export function storeSession(session: StoredSession) {
  localStorage.setItem(TOKEN_KEY, JSON.stringify(session))
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
}

/** Tenta renovar a sessão via refresh token. Usado na abertura do app quando o JWT já expirou. */
export async function refreshStoredSession(): Promise<StoredSession | null> {
  const session = readRawSession()
  if (!session?.refreshToken) return null
  try {
    const { data } = await axios.post<StoredSession>(`${env.apiUrl}/auth/refresh`, {
      refreshToken: session.refreshToken,
    })
    storeSession(data)
    return data
  } catch {
    clearSession()
    return null
  }
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

// Renovação automática: ao receber 401, tenta trocar o refresh token por um
// novo par de tokens UMA vez e repete a requisição original. Se o refresh
// falhar (token revogado/expirado), aí sim desloga o usuário.
let refreshPromise: Promise<boolean> | null = null

async function tryRefreshSession(): Promise<boolean> {
  // Lê sem o filtro de expiração: é justamente com o JWT expirado que o
  // refresh token (vida longa) é usado.
  const session = readRawSession()
  if (!session?.refreshToken) return false
  try {
    const { data } = await axios.post<StoredSession>(`${env.apiUrl}/auth/refresh`, {
      refreshToken: session.refreshToken,
    })
    storeSession(data)
    return true
  } catch {
    return false
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error?.config
    const isAuthEndpoint = typeof originalRequest?.url === 'string' && originalRequest.url.includes('/auth/')
    if (
      axios.isAxiosError(error) &&
      error.response?.status === 401 &&
      !isAuthEndpoint &&
      originalRequest &&
      !originalRequest.__retriedAfterRefresh
    ) {
      originalRequest.__retriedAfterRefresh = true
      // Evita rajadas de refresh paralelas quando várias requisições falham juntas.
      refreshPromise ??= tryRefreshSession().finally(() => { refreshPromise = null })
      if (await refreshPromise) {
        const session = getStoredSession()
        if (session) originalRequest.headers.Authorization = `Bearer ${session.accessToken}`
        return apiClient(originalRequest)
      }
    }
    const normalized = normalizeApiError(error)
    if (normalized.status === 401 && onUnauthorized) {
      onUnauthorized()
    }
    return Promise.reject(normalized)
  },
)
