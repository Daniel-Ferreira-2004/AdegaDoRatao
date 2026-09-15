import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  clearSession,
  getStoredSession,
  setUnauthorizedHandler,
  storeSession,
  type StoredSession,
} from '@/services/api/apiClient'
import { login as loginRequest } from '@/services/auth/authService'

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
    clearSession()
    setSession(null)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(logout)
  }, [logout])

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest({ email, password })
    const session: StoredSession = {
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
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
      return session.permissions.includes(permission)
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
