# Quater.Desktop.Api.Api.PasswordApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiPasswordChangePost**](PasswordApi.md#apipasswordchangepost) | **POST** /api/Password/change | Change password for authenticated user |
| [**ApiPasswordForgotPost**](PasswordApi.md#apipasswordforgotpost) | **POST** /api/Password/forgot | Request password reset token (forgot password) |
| [**ApiPasswordResetPost**](PasswordApi.md#apipasswordresetpost) | **POST** /api/Password/reset | Reset password using a valid token |

<a id="apipasswordchangepost"></a>
# **ApiPasswordChangePost**
> void ApiPasswordChangePost (string? apiVersion = null, ChangePasswordRequest? changePasswordRequest = null)

Change password for authenticated user

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiPasswordChangePostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new PasswordApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var changePasswordRequest = new ChangePasswordRequest?(); // ChangePasswordRequest? |  (optional) 

            try
            {
                // Change password for authenticated user
                apiInstance.ApiPasswordChangePost(apiVersion, changePasswordRequest);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PasswordApi.ApiPasswordChangePost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiPasswordChangePostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Change password for authenticated user
    apiInstance.ApiPasswordChangePostWithHttpInfo(apiVersion, changePasswordRequest);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PasswordApi.ApiPasswordChangePostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **changePasswordRequest** | [**ChangePasswordRequest?**](ChangePasswordRequest?.md) |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apipasswordforgotpost"></a>
# **ApiPasswordForgotPost**
> void ApiPasswordForgotPost (string? apiVersion = null, ForgotPasswordRequest? forgotPasswordRequest = null)

Request password reset token (forgot password)

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiPasswordForgotPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new PasswordApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var forgotPasswordRequest = new ForgotPasswordRequest?(); // ForgotPasswordRequest? |  (optional) 

            try
            {
                // Request password reset token (forgot password)
                apiInstance.ApiPasswordForgotPost(apiVersion, forgotPasswordRequest);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PasswordApi.ApiPasswordForgotPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiPasswordForgotPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Request password reset token (forgot password)
    apiInstance.ApiPasswordForgotPostWithHttpInfo(apiVersion, forgotPasswordRequest);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PasswordApi.ApiPasswordForgotPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **forgotPasswordRequest** | [**ForgotPasswordRequest?**](ForgotPasswordRequest?.md) |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apipasswordresetpost"></a>
# **ApiPasswordResetPost**
> void ApiPasswordResetPost (string? apiVersion = null, ResetPasswordRequest? resetPasswordRequest = null)

Reset password using a valid token

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiPasswordResetPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new PasswordApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var resetPasswordRequest = new ResetPasswordRequest?(); // ResetPasswordRequest? |  (optional) 

            try
            {
                // Reset password using a valid token
                apiInstance.ApiPasswordResetPost(apiVersion, resetPasswordRequest);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling PasswordApi.ApiPasswordResetPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiPasswordResetPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Reset password using a valid token
    apiInstance.ApiPasswordResetPostWithHttpInfo(apiVersion, resetPasswordRequest);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling PasswordApi.ApiPasswordResetPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **resetPasswordRequest** | [**ResetPasswordRequest?**](ResetPasswordRequest?.md) |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

