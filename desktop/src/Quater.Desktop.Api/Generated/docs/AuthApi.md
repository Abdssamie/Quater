# Quater.Desktop.Api.Api.AuthApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiAuthLogoutPost**](AuthApi.md#apiauthlogoutpost) | **POST** /api/Auth/logout | Logout and revoke all tokens for the user |
| [**ApiAuthTokenPost**](AuthApi.md#apiauthtokenpost) | **POST** /api/Auth/token | OAuth2 token endpoint - handles authorization_code and refresh_token grant types.  For authorization code exchange:   POST /api/auth/token   Content-Type: application/x-www-form-urlencoded      grant_type&#x3D;authorization_code&amp;code&#x3D;AUTH_CODE&amp;code_verifier&#x3D;PKCE_VERIFIER&amp;redirect_uri&#x3D;quater://oauth/callback&amp;client_id&#x3D;quater-mobile  For refresh:   POST /api/auth/token   Content-Type: application/x-www-form-urlencoded      grant_type&#x3D;refresh_token&amp;refresh_token&#x3D;YOUR_REFRESH_TOKEN |
| [**ApiAuthUserinfoGet**](AuthApi.md#apiauthuserinfoget) | **GET** /api/Auth/userinfo | Get current user information |

<a id="apiauthlogoutpost"></a>
# **ApiAuthLogoutPost**
> void ApiAuthLogoutPost (string? apiVersion = null)

Logout and revoke all tokens for the user

This endpoint revokes ALL tokens for the user, effectively logging them out from all devices. For per-device logout, use the /revoke endpoint with the specific refresh token.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuthLogoutPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuthApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Logout and revoke all tokens for the user
                apiInstance.ApiAuthLogoutPost(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuthApi.ApiAuthLogoutPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuthLogoutPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Logout and revoke all tokens for the user
    apiInstance.ApiAuthLogoutPostWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuthApi.ApiAuthLogoutPostWithHttpInfo: " + e.Message);
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

<a id="apiauthtokenpost"></a>
# **ApiAuthTokenPost**
> void ApiAuthTokenPost (string? apiVersion = null)

OAuth2 token endpoint - handles authorization_code and refresh_token grant types.  For authorization code exchange:   POST /api/auth/token   Content-Type: application/x-www-form-urlencoded      grant_type=authorization_code&code=AUTH_CODE&code_verifier=PKCE_VERIFIER&redirect_uri=quater://oauth/callback&client_id=quater-mobile  For refresh:   POST /api/auth/token   Content-Type: application/x-www-form-urlencoded      grant_type=refresh_token&refresh_token=YOUR_REFRESH_TOKEN

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuthTokenPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuthApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // OAuth2 token endpoint - handles authorization_code and refresh_token grant types.  For authorization code exchange:   POST /api/auth/token   Content-Type: application/x-www-form-urlencoded      grant_type=authorization_code&code=AUTH_CODE&code_verifier=PKCE_VERIFIER&redirect_uri=quater://oauth/callback&client_id=quater-mobile  For refresh:   POST /api/auth/token   Content-Type: application/x-www-form-urlencoded      grant_type=refresh_token&refresh_token=YOUR_REFRESH_TOKEN
                apiInstance.ApiAuthTokenPost(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuthApi.ApiAuthTokenPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuthTokenPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // OAuth2 token endpoint - handles authorization_code and refresh_token grant types.  For authorization code exchange:   POST /api/auth/token   Content-Type: application/x-www-form-urlencoded      grant_type=authorization_code&code=AUTH_CODE&code_verifier=PKCE_VERIFIER&redirect_uri=quater://oauth/callback&client_id=quater-mobile  For refresh:   POST /api/auth/token   Content-Type: application/x-www-form-urlencoded      grant_type=refresh_token&refresh_token=YOUR_REFRESH_TOKEN
    apiInstance.ApiAuthTokenPostWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuthApi.ApiAuthTokenPostWithHttpInfo: " + e.Message);
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

<a id="apiauthuserinfoget"></a>
# **ApiAuthUserinfoGet**
> UserDto ApiAuthUserinfoGet (string? apiVersion = null)

Get current user information

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuthUserinfoGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuthApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get current user information
                UserDto result = apiInstance.ApiAuthUserinfoGet(apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuthApi.ApiAuthUserinfoGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuthUserinfoGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get current user information
    ApiResponse<UserDto> response = apiInstance.ApiAuthUserinfoGetWithHttpInfo(apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuthApi.ApiAuthUserinfoGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**UserDto**](UserDto.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **401** | Unauthorized |  -  |
| **404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

