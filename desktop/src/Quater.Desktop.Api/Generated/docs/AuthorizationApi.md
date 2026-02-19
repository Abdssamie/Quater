# Quater.Desktop.Api.Api.AuthorizationApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiAuthAuthorizeGet**](AuthorizationApi.md#apiauthauthorizeget) | **GET** /api/auth/authorize | OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE. Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).              Query parameters (validated by OpenIddict before reaching this endpoint):   response_type&#x3D;code (required)   client_id&#x3D;quater-mobile or quater-desktop (required)   redirect_uri&#x3D;... (required, validated against registered URIs)   scope&#x3D;openid email profile offline_access api (optional)   state&#x3D;... (recommended, returned as-is in redirect)   code_challenge&#x3D;... (required when PKCE is enforced)   code_challenge_method&#x3D;S256 (required when PKCE is enforced)              Flow: 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method 2. This controller checks if user is authenticated (via cookie) 3. If not authenticated, returns Challenge to redirect to login 4. If authenticated, creates claims principal and issues authorization code 5. Redirects to redirect_uri with code and state parameters |
| [**ApiAuthAuthorizePost**](AuthorizationApi.md#apiauthauthorizepost) | **POST** /api/auth/authorize | OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE. Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).              Query parameters (validated by OpenIddict before reaching this endpoint):   response_type&#x3D;code (required)   client_id&#x3D;quater-mobile or quater-desktop (required)   redirect_uri&#x3D;... (required, validated against registered URIs)   scope&#x3D;openid email profile offline_access api (optional)   state&#x3D;... (recommended, returned as-is in redirect)   code_challenge&#x3D;... (required when PKCE is enforced)   code_challenge_method&#x3D;S256 (required when PKCE is enforced)              Flow: 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method 2. This controller checks if user is authenticated (via cookie) 3. If not authenticated, returns Challenge to redirect to login 4. If authenticated, creates claims principal and issues authorization code 5. Redirects to redirect_uri with code and state parameters |

<a id="apiauthauthorizeget"></a>
# **ApiAuthAuthorizeGet**
> void ApiAuthAuthorizeGet (string? apiVersion = null)

OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE. Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).              Query parameters (validated by OpenIddict before reaching this endpoint):   response_type=code (required)   client_id=quater-mobile or quater-desktop (required)   redirect_uri=... (required, validated against registered URIs)   scope=openid email profile offline_access api (optional)   state=... (recommended, returned as-is in redirect)   code_challenge=... (required when PKCE is enforced)   code_challenge_method=S256 (required when PKCE is enforced)              Flow: 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method 2. This controller checks if user is authenticated (via cookie) 3. If not authenticated, returns Challenge to redirect to login 4. If authenticated, creates claims principal and issues authorization code 5. Redirects to redirect_uri with code and state parameters

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuthAuthorizeGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuthorizationApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE. Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).              Query parameters (validated by OpenIddict before reaching this endpoint):   response_type=code (required)   client_id=quater-mobile or quater-desktop (required)   redirect_uri=... (required, validated against registered URIs)   scope=openid email profile offline_access api (optional)   state=... (recommended, returned as-is in redirect)   code_challenge=... (required when PKCE is enforced)   code_challenge_method=S256 (required when PKCE is enforced)              Flow: 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method 2. This controller checks if user is authenticated (via cookie) 3. If not authenticated, returns Challenge to redirect to login 4. If authenticated, creates claims principal and issues authorization code 5. Redirects to redirect_uri with code and state parameters
                apiInstance.ApiAuthAuthorizeGet(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuthorizationApi.ApiAuthAuthorizeGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuthAuthorizeGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE. Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).              Query parameters (validated by OpenIddict before reaching this endpoint):   response_type=code (required)   client_id=quater-mobile or quater-desktop (required)   redirect_uri=... (required, validated against registered URIs)   scope=openid email profile offline_access api (optional)   state=... (recommended, returned as-is in redirect)   code_challenge=... (required when PKCE is enforced)   code_challenge_method=S256 (required when PKCE is enforced)              Flow: 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method 2. This controller checks if user is authenticated (via cookie) 3. If not authenticated, returns Challenge to redirect to login 4. If authenticated, creates claims principal and issues authorization code 5. Redirects to redirect_uri with code and state parameters
    apiInstance.ApiAuthAuthorizeGetWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuthorizationApi.ApiAuthAuthorizeGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiauthauthorizepost"></a>
# **ApiAuthAuthorizePost**
> void ApiAuthAuthorizePost (string? apiVersion = null)

OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE. Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).              Query parameters (validated by OpenIddict before reaching this endpoint):   response_type=code (required)   client_id=quater-mobile or quater-desktop (required)   redirect_uri=... (required, validated against registered URIs)   scope=openid email profile offline_access api (optional)   state=... (recommended, returned as-is in redirect)   code_challenge=... (required when PKCE is enforced)   code_challenge_method=S256 (required when PKCE is enforced)              Flow: 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method 2. This controller checks if user is authenticated (via cookie) 3. If not authenticated, returns Challenge to redirect to login 4. If authenticated, creates claims principal and issues authorization code 5. Redirects to redirect_uri with code and state parameters

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuthAuthorizePostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuthorizationApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE. Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).              Query parameters (validated by OpenIddict before reaching this endpoint):   response_type=code (required)   client_id=quater-mobile or quater-desktop (required)   redirect_uri=... (required, validated against registered URIs)   scope=openid email profile offline_access api (optional)   state=... (recommended, returned as-is in redirect)   code_challenge=... (required when PKCE is enforced)   code_challenge_method=S256 (required when PKCE is enforced)              Flow: 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method 2. This controller checks if user is authenticated (via cookie) 3. If not authenticated, returns Challenge to redirect to login 4. If authenticated, creates claims principal and issues authorization code 5. Redirects to redirect_uri with code and state parameters
                apiInstance.ApiAuthAuthorizePost(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuthorizationApi.ApiAuthAuthorizePost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuthAuthorizePostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // OAuth 2.0 authorization endpoint - handles authorization code flow with PKCE. Accepts both GET and POST as per OAuth 2.0 spec (RFC 6749 Section 3.1).              Query parameters (validated by OpenIddict before reaching this endpoint):   response_type=code (required)   client_id=quater-mobile or quater-desktop (required)   redirect_uri=... (required, validated against registered URIs)   scope=openid email profile offline_access api (optional)   state=... (recommended, returned as-is in redirect)   code_challenge=... (required when PKCE is enforced)   code_challenge_method=S256 (required when PKCE is enforced)              Flow: 1. OpenIddict validates client_id, redirect_uri, code_challenge, code_challenge_method 2. This controller checks if user is authenticated (via cookie) 3. If not authenticated, returns Challenge to redirect to login 4. If authenticated, creates claims principal and issues authorization code 5. Redirects to redirect_uri with code and state parameters
    apiInstance.ApiAuthAuthorizePostWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuthorizationApi.ApiAuthAuthorizePostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

