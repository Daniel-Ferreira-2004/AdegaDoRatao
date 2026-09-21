import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  clearSession,
  getStoredSession,
  refreshStoredSession,
  setUnauthorizedHandler,
  storeSession,
  type StoredSession,
} from '@/services/api/apiClient'
import { login as loginRequest, revoke as revokeRequest } from '@/services/auth/authService'

interface AuthContextValue {
  session: StoredSession | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  hasPermission: (permission: string) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<StoredSession | null>(() => getStoredSession())

  const logout = useCallback(() => {
    const current = getStoredSession()
    if (current?.refreshToken) void revokeRequest(current.refreshToken)
    clearSession()
    setSession(null)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(logout)
  }, [logout])

  // Ao abrir o app com o JWT expirado, tenta renovar via refresh token
  // antes de mandar o usuário para a tela de login.
  useEffect(() => {
    if (session) return
    let cancelled = false
    refreshStoredSession().then((renewed) => {
      if (!cancelled && renewed) setSession(renewed)
    })
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest({ email, password })
    const session: StoredSession = {
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
      refreshToken: response.refreshToken,
      userId: response.userId,
      name: response.name,
      role: response.role,
      permissions: response.permissions,
    }
    storeSession(session)
    setSession(session)
  }, [])

  const hasPermission = useCallback(
    (permission: string) => {
      if (!session) return false
      if (session.role === 'ADMIN') return true
      // Sessões antigas (salvas antes de o login retornar permissions)
      // podem não ter o array — trata como vazio em vez de quebrar.
      return (session.permissions ?? []).includes(permission)
    },
    [session],
  )

  const value = useMemo(
    () => ({ session, isAuthenticated: session !== null, login, logout, hasPermission }),
    [session, login, logout, hasPermission],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider')
  return ctx
}
