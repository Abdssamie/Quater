import type { components } from "./schema"

export type ApiConfig = {
  url: string
  timeout: number
}

export type SampleDto = components["schemas"]["SampleDto"]
export type CreateSampleDto = components["schemas"]["CreateSampleDto"]
export type UpdateSampleDto = components["schemas"]["UpdateSampleDto"]
export type SampleDtoPagedResult = components["schemas"]["SampleDtoPagedResult"]

export type ParameterDto = components["schemas"]["ParameterDto"]
export type ParameterDtoPagedResult = components["schemas"]["ParameterDtoPagedResult"]

export type TestResultDto = components["schemas"]["TestResultDto"]
export type CreateTestResultDto = components["schemas"]["CreateTestResultDto"]
export type UpdateTestResultDto = components["schemas"]["UpdateTestResultDto"]
export type TestResultDtoPagedResult = components["schemas"]["TestResultDtoPagedResult"]

export type AuthTokenResponse = {
  access_token: string
  token_type: string
  expires_in: number
  refresh_token?: string
  scope?: string
  id_token?: string
}

export type UserInfoResponse = {
  id: string
  email?: string | null
  userName?: string | null
  role: string
  labId: string
  isActive: boolean
  lastLogin?: string | null
}
