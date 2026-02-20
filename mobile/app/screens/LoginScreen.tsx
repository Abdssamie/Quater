import { FC, useEffect, useState } from "react"
import { ActivityIndicator, TextStyle, ViewStyle } from "react-native"

import { makeRedirectUri, ResponseType, useAuthRequest } from "expo-auth-session"
import * as WebBrowser from "expo-web-browser"

import { Button } from "@/components/Button"
import { Screen } from "@/components/Screen"
import { Text } from "@/components/Text"
import Config from "@/config"
import { useAuth } from "@/context/AuthContext"
import type { AppStackScreenProps } from "@/navigators/navigationTypes"
import { api } from "@/services/api"
import { useAppTheme } from "@/theme/context"
import type { ThemedStyle } from "@/theme/types"

// Required on iOS: completes the auth session when the app is brought back
// into the foreground after the system browser redirect.
WebBrowser.maybeCompleteAuthSession()

const REDIRECT_URI = makeRedirectUri({
  scheme: Config.OAUTH_REDIRECT_SCHEME,
  path: Config.OAUTH_REDIRECT_PATH,
})

const DISCOVERY = {
  authorizationEndpoint: `${Config.API_URL}/api/auth/authorize`,
  tokenEndpoint: `${Config.API_URL}/api/auth/token`,
}

interface LoginScreenProps extends AppStackScreenProps<"Login"> {}

export const LoginScreen: FC<LoginScreenProps> = () => {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | undefined>()

  const { setAuthToken, setRefreshToken, setTokenExpiry, setLabId, setLabRole } = useAuth()

  const {
    themed,
    theme: { colors },
  } = useAppTheme()

  const [request, response, promptAsync] = useAuthRequest(
    {
      clientId: Config.OAUTH_CLIENT_ID,
      redirectUri: REDIRECT_URI,
      scopes: Config.OAUTH_SCOPES,
      responseType: ResponseType.Code,
      usePKCE: true,
    },
    DISCOVERY,
  )

  // Handle the authorization response once the browser redirects back.
  useEffect(() => {
    if (!response) return

    if (response.type === "error") {
      setError(response.error?.message ?? "Authorization failed. Please try again.")
      setIsLoading(false)
      return
    }

    if (response.type === "cancel") {
      setIsLoading(false)
      return
    }

    if (response.type !== "success") return

    const { code } = response.params
    const codeVerifier = request?.codeVerifier

    if (!code || !codeVerifier) {
      setError("Authorization response is missing required parameters.")
      setIsLoading(false)
      return
    }

    void exchangeCode(code, codeVerifier)
  }, [response]) // eslint-disable-line react-hooks/exhaustive-deps

  async function exchangeCode(code: string, codeVerifier: string) {
    setIsLoading(true)
    setError(undefined)

    const tokenResult = await api.exchangeAuthorizationCode({
      code,
      codeVerifier,
      redirectUri: REDIRECT_URI,
      clientId: Config.OAUTH_CLIENT_ID,
    })

    if (tokenResult.kind !== "ok") {
      setError("Sign in failed. Please try again.")
      setIsLoading(false)
      return
    }

    const { access_token, refresh_token, expires_in } = tokenResult.data
    setAuthToken(access_token)
    if (refresh_token) setRefreshToken(refresh_token)
    setTokenExpiry(Date.now() + expires_in * 1000)

    const userInfoResult = await api.getUserInfo()
    if (userInfoResult.kind === "ok") {
      setLabId(userInfoResult.data.labId)
      setLabRole(userInfoResult.data.role)
    }

    setIsLoading(false)
  }

  async function handleSignIn() {
    setError(undefined)
    setIsLoading(true)
    await promptAsync()
    // Loading state is cleared in the useEffect above once response arrives.
  }

  return (
    <Screen
      preset="fixed"
      contentContainerStyle={themed($screenContentContainer)}
      safeAreaEdges={["top", "bottom"]}
    >
      <Text preset="heading" tx="loginScreen:logIn" style={themed($heading)} />
      <Text preset="subheading" tx="loginScreen:enterDetails" style={themed($subheading)} />

      {error !== undefined && (
        <Text size="sm" style={[themed($errorText), { color: colors.error }]}>
          {error}
        </Text>
      )}

      {isLoading ? (
        <ActivityIndicator
          size="large"
          color={colors.tint}
          style={themed($activityIndicator)}
          testID="login-loading"
        />
      ) : (
        <Button
          testID="login-button"
          tx="loginScreen:tapToLogIn"
          style={themed($signInButton)}
          preset="reversed"
          disabled={!request}
          onPress={handleSignIn}
        />
      )}
    </Screen>
  )
}

const $screenContentContainer: ThemedStyle<ViewStyle> = ({ spacing }) => ({
  flex: 1,
  justifyContent: "center",
  paddingHorizontal: spacing.lg,
  paddingVertical: spacing.xxl,
})

const $heading: ThemedStyle<TextStyle> = ({ spacing }) => ({
  marginBottom: spacing.sm,
})

const $subheading: ThemedStyle<TextStyle> = ({ spacing }) => ({
  marginBottom: spacing.xl,
})

const $errorText: ThemedStyle<TextStyle> = ({ spacing }) => ({
  marginBottom: spacing.md,
})

const $activityIndicator: ThemedStyle<ViewStyle> = ({ spacing }) => ({
  marginTop: spacing.xl,
})

const $signInButton: ThemedStyle<ViewStyle> = ({ spacing }) => ({
  marginTop: spacing.xs,
})
