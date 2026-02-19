import { loadString, remove, saveString } from "@/utils/storage"

const ACCESS_TOKEN_KEY = "auth.accessToken"
const REFRESH_TOKEN_KEY = "auth.refreshToken"
const LAB_ID_KEY = "auth.labId"

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
  clear(): void {
    remove(ACCESS_TOKEN_KEY)
    remove(REFRESH_TOKEN_KEY)
    remove(LAB_ID_KEY)
  },
}
