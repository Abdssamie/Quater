import { ApiResponse, ApisauceInstance, create } from "apisauce"

import Config from "@/config"
import { authStore } from "@/stores/authStore"

import { GeneralApiProblem, getGeneralApiProblem } from "./apiProblem"
import type {
  ApiConfig,
  AuthTokenResponse,
  CreateSampleDto,
  CreateTestResultDto,
  ParameterDto,
  ParameterDtoPagedResult,
  SampleDto,
  SampleDtoPagedResult,
  TestResultDto,
  TestResultDtoPagedResult,
  UpdateSampleDto,
  UpdateTestResultDto,
  UserInfoResponse,
} from "./types"

export const DEFAULT_API_CONFIG: ApiConfig = {
  url: Config.API_URL,
  timeout: 10000,
}

const TOKEN_ENDPOINT = "/api/auth/token"
const USERINFO_ENDPOINT = "/api/auth/userinfo"
const LOGOUT_ENDPOINT = "/api/auth/logout"

type AuthCallbacks = {
  /** Called on 401. Should refresh the access token and return true on success. */
  onRefreshToken: () => Promise<boolean>
  /** Called when a refresh attempt fails — clears all local auth state. */
  onLogout: () => void
}

// Extend the axios request config type to support a retry flag.
declare module "axios" {
  interface InternalAxiosRequestConfig {
    _retry?: boolean
  }
}

export class Api {
  apisauce: ApisauceInstance
  config: ApiConfig

  constructor(config: ApiConfig = DEFAULT_API_CONFIG) {
    this.config = config
    this.apisauce = create({
      baseURL: this.config.url,
      timeout: this.config.timeout,
      headers: {
        Accept: "application/json",
      },
    })

    this.apisauce.addRequestTransform((request) => {
      const token = authStore.getAccessToken()
      if (token) {
        request.headers = {
          ...request.headers,
          Authorization: `Bearer ${token}`,
        }
      }

      const labId = authStore.getLabId()
      if (labId) {
        request.headers = {
          ...request.headers,
          "X-Lab-Id": labId,
        }
      }
    })
  }

  /**
   * Wire up refresh + logout callbacks and register an axios response interceptor
   * that automatically retries requests after a successful token refresh on 401.
   *
   * Call this once from AuthContext on mount.
   */
  setAuthCallbacks(callbacks: AuthCallbacks): void {
    const axiosInstance = this.apisauce.axiosInstance

    axiosInstance.interceptors.response.use(
      (response) => response,
      async (error) => {
        const original = error.config

        // Only retry once, and never retry the token endpoint itself
        // (avoids infinite loops when the refresh token is also invalid).
        if (
          error.response?.status === 401 &&
          !original._retry &&
          !original.url?.includes(TOKEN_ENDPOINT)
        ) {
          original._retry = true

          const refreshed = await callbacks.onRefreshToken()
          if (refreshed) {
            // Token has been updated in authStore — pick it up for the retry.
            const newToken = authStore.getAccessToken()
            if (newToken) {
              original.headers["Authorization"] = `Bearer ${newToken}`
            }
            return axiosInstance(original)
          }

          // Refresh failed — force logout so the user is sent back to LoginScreen.
          callbacks.onLogout()
        }

        return Promise.reject(error)
      },
    )
  }

  async exchangeAuthorizationCode(params: {
    code: string
    codeVerifier: string
    redirectUri: string
    clientId: string
  }): Promise<{ kind: "ok"; data: AuthTokenResponse } | GeneralApiProblem> {
    const formBody = new URLSearchParams({
      grant_type: "authorization_code",
      code: params.code,
      code_verifier: params.codeVerifier,
      redirect_uri: params.redirectUri,
      client_id: params.clientId,
    })

    const response: ApiResponse<AuthTokenResponse> = await this.apisauce.post(
      TOKEN_ENDPOINT,
      formBody.toString(),
      {
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
      },
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async refreshToken(
    refreshToken: string,
  ): Promise<{ kind: "ok"; data: AuthTokenResponse } | GeneralApiProblem> {
    const formBody = new URLSearchParams({
      grant_type: "refresh_token",
      refresh_token: refreshToken,
    })

    const response: ApiResponse<AuthTokenResponse> = await this.apisauce.post(
      TOKEN_ENDPOINT,
      formBody.toString(),
      {
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
      },
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getUserInfo(): Promise<{ kind: "ok"; data: UserInfoResponse } | GeneralApiProblem> {
    const response: ApiResponse<UserInfoResponse> = await this.apisauce.get(USERINFO_ENDPOINT)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async logout(): Promise<
    { kind: "ok"; data: { message: string; tokensRevoked: number } } | GeneralApiProblem
  > {
    const response: ApiResponse<{ message: string; tokensRevoked: number }> =
      await this.apisauce.post(LOGOUT_ENDPOINT)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getSamples(
    pageNumber = 1,
    pageSize = 50,
  ): Promise<{ kind: "ok"; data: SampleDtoPagedResult } | GeneralApiProblem> {
    const response: ApiResponse<SampleDtoPagedResult> = await this.apisauce.get("/api/samples", {
      pageNumber,
      pageSize,
    })

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getSamplesByLab(
    labId: string,
    pageNumber = 1,
    pageSize = 50,
  ): Promise<{ kind: "ok"; data: SampleDtoPagedResult } | GeneralApiProblem> {
    const response: ApiResponse<SampleDtoPagedResult> = await this.apisauce.get(
      `/api/samples/by-lab/${labId}`,
      {
        pageNumber,
        pageSize,
      },
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getSampleById(id: string): Promise<{ kind: "ok"; data: SampleDto } | GeneralApiProblem> {
    const response: ApiResponse<SampleDto> = await this.apisauce.get(`/api/samples/${id}`)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async createSample(
    dto: CreateSampleDto,
  ): Promise<{ kind: "ok"; data: SampleDto } | GeneralApiProblem> {
    const response: ApiResponse<SampleDto> = await this.apisauce.post("/api/samples", dto)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async updateSample(
    id: string,
    dto: UpdateSampleDto,
  ): Promise<{ kind: "ok"; data: SampleDto } | GeneralApiProblem> {
    const response: ApiResponse<SampleDto> = await this.apisauce.put(`/api/samples/${id}`, dto)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async deleteSample(id: string): Promise<{ kind: "ok" } | GeneralApiProblem> {
    const response = await this.apisauce.delete(`/api/samples/${id}`)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    return { kind: "ok" }
  }

  async getParameters(
    pageNumber = 1,
    pageSize = 50,
  ): Promise<{ kind: "ok"; data: ParameterDtoPagedResult } | GeneralApiProblem> {
    const response: ApiResponse<ParameterDtoPagedResult> = await this.apisauce.get(
      "/api/parameters",
      {
        pageNumber,
        pageSize,
      },
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getActiveParameters(): Promise<{ kind: "ok"; data: ParameterDto[] } | GeneralApiProblem> {
    const response: ApiResponse<ParameterDto[]> = await this.apisauce.get("/api/parameters/active")

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getParameterById(
    id: string,
  ): Promise<{ kind: "ok"; data: ParameterDto } | GeneralApiProblem> {
    const response: ApiResponse<ParameterDto> = await this.apisauce.get(`/api/parameters/${id}`)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getTestResults(
    pageNumber = 1,
    pageSize = 50,
  ): Promise<{ kind: "ok"; data: TestResultDtoPagedResult } | GeneralApiProblem> {
    const response: ApiResponse<TestResultDtoPagedResult> = await this.apisauce.get(
      "/api/testresults",
      {
        pageNumber,
        pageSize,
      },
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getTestResultsBySample(
    sampleId: string,
    pageNumber = 1,
    pageSize = 50,
  ): Promise<{ kind: "ok"; data: TestResultDtoPagedResult } | GeneralApiProblem> {
    const response: ApiResponse<TestResultDtoPagedResult> = await this.apisauce.get(
      `/api/testresults/by-sample/${sampleId}`,
      {
        pageNumber,
        pageSize,
      },
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async getTestResultById(
    id: string,
  ): Promise<{ kind: "ok"; data: TestResultDto } | GeneralApiProblem> {
    const response: ApiResponse<TestResultDto> = await this.apisauce.get(`/api/testresults/${id}`)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async createTestResult(
    dto: CreateTestResultDto,
  ): Promise<{ kind: "ok"; data: TestResultDto } | GeneralApiProblem> {
    const response: ApiResponse<TestResultDto> = await this.apisauce.post("/api/testresults", dto)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async updateTestResult(
    id: string,
    dto: UpdateTestResultDto,
  ): Promise<{ kind: "ok"; data: TestResultDto } | GeneralApiProblem> {
    const response: ApiResponse<TestResultDto> = await this.apisauce.put(
      `/api/testresults/${id}`,
      dto,
    )

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    if (!response.data) return { kind: "bad-data" }

    return { kind: "ok", data: response.data }
  }

  async deleteTestResult(id: string): Promise<{ kind: "ok" } | GeneralApiProblem> {
    const response = await this.apisauce.delete(`/api/testresults/${id}`)

    if (!response.ok) {
      const problem = getGeneralApiProblem(response)
      if (problem) return problem
    }

    return { kind: "ok" }
  }
}

export const api = new Api()
