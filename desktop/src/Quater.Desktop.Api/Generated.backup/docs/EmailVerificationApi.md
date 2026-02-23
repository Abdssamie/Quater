# Quater.Desktop.Api.Api.EmailVerificationApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiEmailVerificationResendPost**](EmailVerificationApi.md#apiemailverificationresendpost) | **POST** /api/email_verification/resend | Resend the email verification link |
| [**ApiEmailVerificationVerifyPost**](EmailVerificationApi.md#apiemailverificationverifypost) | **POST** /api/email_verification/verify | Verify user email address using a token |

<a id="apiemailverificationresendpost"></a>
# **ApiEmailVerificationResendPost**
> void ApiEmailVerificationResendPost (string? apiVersion = null, ResendVerificationRequest? resendVerificationRequest = null)

Resend the email verification link

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiEmailVerificationResendPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new EmailVerificationApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var resendVerificationRequest = new ResendVerificationRequest?(); // ResendVerificationRequest? |  (optional) 

            try
            {
                // Resend the email verification link
                apiInstance.ApiEmailVerificationResendPost(apiVersion, resendVerificationRequest);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling EmailVerificationApi.ApiEmailVerificationResendPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiEmailVerificationResendPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Resend the email verification link
    apiInstance.ApiEmailVerificationResendPostWithHttpInfo(apiVersion, resendVerificationRequest);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling EmailVerificationApi.ApiEmailVerificationResendPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **resendVerificationRequest** | [**ResendVerificationRequest?**](ResendVerificationRequest?.md) |  | [optional]  |

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

<a id="apiemailverificationverifypost"></a>
# **ApiEmailVerificationVerifyPost**
> void ApiEmailVerificationVerifyPost (string? apiVersion = null, VerifyEmailRequest? verifyEmailRequest = null)

Verify user email address using a token

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiEmailVerificationVerifyPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new EmailVerificationApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var verifyEmailRequest = new VerifyEmailRequest?(); // VerifyEmailRequest? |  (optional) 

            try
            {
                // Verify user email address using a token
                apiInstance.ApiEmailVerificationVerifyPost(apiVersion, verifyEmailRequest);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling EmailVerificationApi.ApiEmailVerificationVerifyPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiEmailVerificationVerifyPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Verify user email address using a token
    apiInstance.ApiEmailVerificationVerifyPostWithHttpInfo(apiVersion, verifyEmailRequest);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling EmailVerificationApi.ApiEmailVerificationVerifyPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **verifyEmailRequest** | [**VerifyEmailRequest?**](VerifyEmailRequest?.md) |  | [optional]  |

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

