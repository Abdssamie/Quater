import { createContext, FC, PropsWithChildren, useCallback, useContext, useMemo, useState } from "react"

import { authStore } from "@/stores/authStore"

export type AuthContextType = {
  isAuthenticated: boolean
  authToken?: string
  authEmail?: string
  labId?: string
  refreshToken?: string
  setAuthToken: (token?: string) => void
  setRefreshToken: (token?: string) => void
  setAuthEmail: (email: string) => void
  setLabId: (labId?: string) => void
  logout: () => void
  validationError: string
}

export const AuthContext = createContext<AuthContextType | null>(null)

export interface AuthProviderProps {}

export const AuthProvider: FC<PropsWithChildren<AuthProviderProps>> = ({ children }) => {
  const [authToken, setAuthTokenState] = useState<string | undefined>(
    authStore.getAccessToken() ?? undefined,
  )
  const [refreshToken, setRefreshTokenState] = useState<string | undefined>(
    authStore.getRefreshToken() ?? undefined,
  )
  const [labId, setLabIdState] = useState<string | undefined>(authStore.getLabId() ?? undefined)
  const [authEmail, setAuthEmail] = useState<string | undefined>(undefined)

  const setAuthToken = useCallback((token?: string) => {
    authStore.setAccessToken(token)
    setAuthTokenState(token)
  }, [])

  const setRefreshToken = useCallback((token?: string) => {
    authStore.setRefreshToken(token)
    setRefreshTokenState(token)
  }, [])

  const setLabId = useCallback((nextLabId?: string) => {
    authStore.setLabId(nextLabId)
    setLabIdState(nextLabId)
  }, [])

  const logout = useCallback(() => {
    authStore.clear()
    setAuthTokenState(undefined)
    setRefreshTokenState(undefined)
    setLabIdState(undefined)
    setAuthEmail("")
  }, [])

  const validationError = useMemo(() => {
    if (!authEmail || authEmail.length === 0) return "can't be blank"
    if (authEmail.length < 6) return "must be at least 6 characters"
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(authEmail)) return "must be a valid email address"
    return ""
  }, [authEmail])

  const value = {
    isAuthenticated: !!authToken,
    authToken,
    refreshToken,
    authEmail,
    labId,
    setAuthToken,
    setRefreshToken,
    setAuthEmail,
    setLabId,
    logout,
    validationError,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) throw new Error("useAuth must be used within an AuthProvider")
  return context
}
