import {
  createContext,
  FC,
  PropsWithChildren,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react"

import { api } from "@/services/api"
import { authStore } from "@/stores/authStore"

export type AuthContextType = {
  isAuthenticated: boolean
  authToken?: string
  refreshToken?: string
  labId?: string
  /** User role within the current lab: "Admin" | "Technician" | "Viewer" */
  labRole?: string
  /** Unix timestamp (ms) when the current access token expires */
  tokenExpiry?: number
  setAuthToken: (token?: string) => void
  setRefreshToken: (token?: string) => void
  setLabId: (labId?: string) => void
  setLabRole: (role?: string) => void
  setTokenExpiry: (expiry?: number) => void
  /**
   * Attempts to refresh the access token using the stored refresh token.
   * Updates all token state on success.
   * Returns true if the refresh succeeded, false otherwise.
   */
  refresh: () => Promise<boolean>
  logout: () => void
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
  const [labRole, setLabRoleState] = useState<string | undefined>(
    authStore.getLabRole() ?? undefined,
  )
  const [tokenExpiry, setTokenExpiryState] = useState<number | undefined>(
    authStore.getTokenExpiry() ?? undefined,
  )

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

  const setLabRole = useCallback((role?: string) => {
    authStore.setLabRole(role)
    setLabRoleState(role)
  }, [])

  const setTokenExpiry = useCallback((expiry?: number) => {
    authStore.setTokenExpiry(expiry)
    setTokenExpiryState(expiry)
  }, [])

  const logout = useCallback(() => {
    // Fire-and-forget: revoke tokens on the server. We clear local state regardless.
    api.logout().catch(() => {})
    authStore.clear()
    setAuthTokenState(undefined)
    setRefreshTokenState(undefined)
    setLabIdState(undefined)
    setLabRoleState(undefined)
    setTokenExpiryState(undefined)
  }, [])

  const refresh = useCallback(async (): Promise<boolean> => {
    const storedRefreshToken = authStore.getRefreshToken()
    if (!storedRefreshToken) return false

    const result = await api.refreshToken(storedRefreshToken)
    if (result.kind !== "ok") return false

    const { access_token, refresh_token, expires_in } = result.data

    authStore.setAccessToken(access_token)
    setAuthTokenState(access_token)

    if (refresh_token) {
      authStore.setRefreshToken(refresh_token)
      setRefreshTokenState(refresh_token)
    }

    const expiresAt = Date.now() + expires_in * 1000
    authStore.setTokenExpiry(expiresAt)
    setTokenExpiryState(expiresAt)

    return true
  }, [])

  // Register the refresh + logout callbacks on the API client once on mount.
  // The axios interceptor uses these to auto-retry requests on 401.
  useEffect(() => {
    api.setAuthCallbacks({ onRefreshToken: refresh, onLogout: logout })
  }, [refresh, logout])

  const value = useMemo<AuthContextType>(
    () => ({
      isAuthenticated: !!authToken,
      authToken,
      refreshToken,
      labId,
      labRole,
      tokenExpiry,
      setAuthToken,
      setRefreshToken,
      setLabId,
      setLabRole,
      setTokenExpiry,
      refresh,
      logout,
    }),
    [
      authToken,
      refreshToken,
      labId,
      labRole,
      tokenExpiry,
      setAuthToken,
      setRefreshToken,
      setLabId,
      setLabRole,
      setTokenExpiry,
      refresh,
      logout,
    ],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) throw new Error("useAuth must be used within an AuthProvider")
  return context
}
