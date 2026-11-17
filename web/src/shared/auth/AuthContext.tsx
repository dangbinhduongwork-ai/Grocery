import React, { createContext, useContext, useEffect, useState, useCallback } from 'react'
import { api, setAccessToken, setOnAuthFailure, requestSilentRefresh, type AuthUser } from '@/shared/api/client'

export type { AuthUser }

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  isAuthenticated: boolean
  isStaff: boolean
  isAdmin: boolean
  sessionNotice: string | null
  clearSessionNotice: () => void
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>
  register: (email: string, password: string, fullName: string, phone: string) => Promise<{ success: boolean; error?: string }>
  logout: () => Promise<void>
  hasPermission: (permission: string) => boolean
  refreshProfile: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [sessionNotice, setSessionNotice] = useState<string | null>(null)

  const fetchProfile = useCallback(async (): Promise<boolean> => {
    try {
      const { data, error } = await api.GET('/api/v1/auth/me')
      if (data && !error) {
        setUser(data)
        return true
      }
      setUser(null)
      return false
    } catch {
      setUser(null)
      return false
    }
  }, [])

  // Attempt silent refresh on initial application mount
  useEffect(() => {
    let isMounted = true

    async function initAuth() {
      try {
        const refreshResult = await requestSilentRefresh()
        if (isMounted && refreshResult?.accessToken) {
          if (refreshResult.user) {
            setUser(refreshResult.user)
          } else {
            await fetchProfile()
          }
        } else if (isMounted) {
          setUser(null)
        }
      } catch {
        if (isMounted) {
          setUser(null)
          setAccessToken(null)
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    setOnAuthFailure(() => {
      if (isMounted) {
        setUser((prev) => {
          if (prev) {
            setSessionNotice('Phiên đăng nhập đã hết hạn để đảm bảo an toàn. Vui lòng đăng nhập lại.')
          }
          return null
        })
      }
    })

    void initAuth()

    return () => {
      isMounted = false
    }
  }, [fetchProfile])

  const login = async (email: string, password: string): Promise<{ success: boolean; error?: string }> => {
    try {
      const { data, error } = await api.POST('/api/v1/auth/login', {
        body: { email, password },
      })

      if (error || !data) {
        const errorDetail = (error as { detail?: string })?.detail ?? 'Đăng nhập không thành công.'
        return { success: false, error: errorDetail }
      }

      setAccessToken(data.accessToken)
      setSessionNotice(null)
      if (data.user) {
        setUser(data.user)
      } else {
        await fetchProfile()
      }

      return { success: true }
    } catch (err) {
      return { success: false, error: err instanceof Error ? err.message : 'Lỗi kết nối máy chủ.' }
    }
  }

  const register = async (
    email: string,
    password: string,
    fullName: string,
    phone: string
  ): Promise<{ success: boolean; error?: string }> => {
    try {
      const { error } = await api.POST('/api/v1/auth/register', {
        body: { email, password, fullName, phone },
      })

      if (error) {
        const errorDetail = (error as { detail?: string })?.detail ?? 'Đăng ký không thành công.'
        return { success: false, error: errorDetail }
      }

      return { success: true }
    } catch (err) {
      return { success: false, error: err instanceof Error ? err.message : 'Lỗi kết nối máy chủ.' }
    }
  }

  const logout = async (): Promise<void> => {
    try {
      await api.POST('/api/v1/auth/logout')
    } finally {
      setAccessToken(null)
      setUser(null)
    }
  }

  const hasPermission = useCallback((permission: string): boolean => {
    if (!user) return false
    return user.permissions.includes(permission) || user.roles.includes('Admin')
  }, [user])

  const isStaff = user?.roles.some(r => ['Staff', 'Manager', 'Admin'].includes(r)) ?? false
  const isAdmin = user?.roles.includes('Admin') ?? false

  const value: AuthContextValue = {
    user,
    isLoading,
    isAuthenticated: !!user,
    isStaff,
    isAdmin,
    sessionNotice,
    clearSessionNotice: () => setSessionNotice(null),
    login,
    register,
    logout,
    hasPermission,
    refreshProfile: async () => {
      await fetchProfile()
    },
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
