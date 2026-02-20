export interface ConfigBaseProps {
  persistNavigation: "always" | "dev" | "prod" | "never"
  catchErrors: "always" | "dev" | "prod" | "never"
  exitRoutes: string[]
  API_URL: string
  /** OAuth2 client ID registered in the backend OpenIddict seeder */
  OAUTH_CLIENT_ID: string
  /** Custom URI scheme used for the OAuth2 redirect (matches backend seeded redirect URI) */
  OAUTH_REDIRECT_SCHEME: string
  /** Path segment of the OAuth2 redirect URI */
  OAUTH_REDIRECT_PATH: string
  /** OAuth2 scopes requested during authorization */
  OAUTH_SCOPES: string[]
}

export type PersistNavigationConfig = ConfigBaseProps["persistNavigation"]

const BaseConfig: Omit<ConfigBaseProps, "API_URL"> = {
  // This feature is particularly useful in development mode, but
  // can be used in production as well if you prefer.
  persistNavigation: "dev",

  /**
   * Only enable if we're catching errors in the right environment
   */
  catchErrors: "always",

  /**
   * This is a list of all the route names that will exit the app if the back button
   * is pressed while in that screen. Only affects Android.
   */
  exitRoutes: ["Welcome"],

  /**
   * OAuth2 / OpenIddict configuration.
   * These values match the quater-mobile-client seeded in the backend.
   * The client is public (no secret) — safe to include in the app bundle.
   */
  OAUTH_CLIENT_ID: "quater-mobile-client",
  OAUTH_REDIRECT_SCHEME: "quater",
  OAUTH_REDIRECT_PATH: "oauth/callback",
  OAUTH_SCOPES: ["openid", "email", "profile", "offline_access", "api"],
}

export default BaseConfig
