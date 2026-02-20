import { loadString, remove, saveString } from "@/utils/storage"

const ACCESS_TOKEN_KEY = "auth.accessToken"
const REFRESH_TOKEN_KEY = "auth.refreshToken"
const LAB_ID_KEY = "auth.labId"
const TOKEN_EXPIRY_KEY = "auth.tokenExpiry"
const LAB_ROLE_KEY = "auth.labRole"

export const authStore = {
  getAccessToken(): string | null {
    return loadString(ACCESS_TOKEN_KEY)
  },
  setAccessToken(token?: string): void {
    if (token) {
      saveString(ACCESS_TOKEN_KEY, token)
      return
    }
    remove(ACCESS_TOKEN_KEY)
  },

  getRefreshToken(): string | null {
    return loadString(REFRESH_TOKEN_KEY)
  },
  setRefreshToken(token?: string): void {
    if (token) {
      saveString(REFRESH_TOKEN_KEY, token)
      return
    }
    remove(REFRESH_TOKEN_KEY)
  },

  getLabId(): string | null {
    return loadString(LAB_ID_KEY)
  },
  setLabId(labId?: string): void {
    if (labId) {
      saveString(LAB_ID_KEY, labId)
      return
    }
    remove(LAB_ID_KEY)
  },

  /** Unix timestamp in ms: Date.now() + expires_in * 1000 */
  getTokenExpiry(): number | null {
    const raw = loadString(TOKEN_EXPIRY_KEY)
    if (!raw) return null
    const parsed = parseInt(raw, 10)
    return isNaN(parsed) ? null : parsed
  },
  setTokenExpiry(expiresAt?: number): void {
    if (expiresAt !== undefined) {
      saveString(TOKEN_EXPIRY_KEY, String(expiresAt))
      return
    }
    remove(TOKEN_EXPIRY_KEY)
  },

  /** User role within the lab context: "Admin" | "Technician" | "Viewer" */
  getLabRole(): string | null {
    return loadString(LAB_ROLE_KEY)
  },
  setLabRole(role?: string): void {
    if (role) {
      saveString(LAB_ROLE_KEY, role)
      return
    }
    remove(LAB_ROLE_KEY)
  },

  clear(): void {
    remove(ACCESS_TOKEN_KEY)
    remove(REFRESH_TOKEN_KEY)
    remove(LAB_ID_KEY)
    remove(TOKEN_EXPIRY_KEY)
    remove(LAB_ROLE_KEY)
  },
}
