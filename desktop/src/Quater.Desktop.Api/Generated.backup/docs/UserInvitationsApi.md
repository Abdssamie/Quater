# Quater.Desktop.Api.Api.UserInvitationsApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiInvitationsAcceptPost**](UserInvitationsApi.md#apiinvitationsacceptpost) | **POST** /api/invitations/accept | Accept an invitation and activate the user. |
| [**ApiInvitationsGet**](UserInvitationsApi.md#apiinvitationsget) | **GET** /api/invitations | Get pending invitations with pagination. |
| [**ApiInvitationsInvitationIdDelete**](UserInvitationsApi.md#apiinvitationsinvitationiddelete) | **DELETE** /api/invitations/{invitationId} | Revoke a pending invitation. |
| [**ApiInvitationsPost**](UserInvitationsApi.md#apiinvitationspost) | **POST** /api/invitations | Create a new user invitation. |
| [**ApiInvitationsTokenGet**](UserInvitationsApi.md#apiinvitationstokenget) | **GET** /api/invitations/{token} | Get an invitation by token. |

<a id="apiinvitationsacceptpost"></a>
# **ApiInvitationsAcceptPost**
> UserInvitationDto ApiInvitationsAcceptPost (string? apiVersion = null, AcceptInvitationDto? acceptInvitationDto = null)

Accept an invitation and activate the user.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiInvitationsAcceptPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserInvitationsApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var acceptInvitationDto = new AcceptInvitationDto?(); // AcceptInvitationDto? |  (optional) 

            try
            {
                // Accept an invitation and activate the user.
                UserInvitationDto result = apiInstance.ApiInvitationsAcceptPost(apiVersion, acceptInvitationDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsAcceptPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiInvitationsAcceptPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Accept an invitation and activate the user.
    ApiResponse<UserInvitationDto> response = apiInstance.ApiInvitationsAcceptPostWithHttpInfo(apiVersion, acceptInvitationDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsAcceptPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **acceptInvitationDto** | [**AcceptInvitationDto?**](AcceptInvitationDto?.md) |  | [optional]  |

### Return type

[**UserInvitationDto**](UserInvitationDto.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |
| **404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiinvitationsget"></a>
# **ApiInvitationsGet**
> UserInvitationDtoPagedResult ApiInvitationsGet (int? page = null, int? pageSize = null, string? apiVersion = null)

Get pending invitations with pagination.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiInvitationsGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserInvitationsApi(config);
            var page = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 20;  // int? |  (optional)  (default to 20)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get pending invitations with pagination.
                UserInvitationDtoPagedResult result = apiInstance.ApiInvitationsGet(page, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiInvitationsGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get pending invitations with pagination.
    ApiResponse<UserInvitationDtoPagedResult> response = apiInstance.ApiInvitationsGetWithHttpInfo(page, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **page** | **int?** |  | [optional] [default to 1] |
| **pageSize** | **int?** |  | [optional] [default to 20] |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**UserInvitationDtoPagedResult**](UserInvitationDtoPagedResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiinvitationsinvitationiddelete"></a>
# **ApiInvitationsInvitationIdDelete**
> void ApiInvitationsInvitationIdDelete (Guid invitationId, string? apiVersion = null)

Revoke a pending invitation.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiInvitationsInvitationIdDeleteExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserInvitationsApi(config);
            var invitationId = "invitationId_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Revoke a pending invitation.
                apiInstance.ApiInvitationsInvitationIdDelete(invitationId, apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsInvitationIdDelete: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiInvitationsInvitationIdDeleteWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Revoke a pending invitation.
    apiInstance.ApiInvitationsInvitationIdDeleteWithHttpInfo(invitationId, apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsInvitationIdDeleteWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **invitationId** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **204** | No Content |  -  |
| **404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiinvitationspost"></a>
# **ApiInvitationsPost**
> UserInvitationDto ApiInvitationsPost (string? apiVersion = null, CreateUserInvitationDto? createUserInvitationDto = null)

Create a new user invitation.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiInvitationsPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserInvitationsApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var createUserInvitationDto = new CreateUserInvitationDto?(); // CreateUserInvitationDto? |  (optional) 

            try
            {
                // Create a new user invitation.
                UserInvitationDto result = apiInstance.ApiInvitationsPost(apiVersion, createUserInvitationDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiInvitationsPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create a new user invitation.
    ApiResponse<UserInvitationDto> response = apiInstance.ApiInvitationsPostWithHttpInfo(apiVersion, createUserInvitationDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **createUserInvitationDto** | [**CreateUserInvitationDto?**](CreateUserInvitationDto?.md) |  | [optional]  |

### Return type

[**UserInvitationDto**](UserInvitationDto.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Created |  -  |
| **400** | Bad Request |  -  |
| **409** | Conflict |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiinvitationstokenget"></a>
# **ApiInvitationsTokenGet**
> UserInvitationDto ApiInvitationsTokenGet (string token, string? apiVersion = null)

Get an invitation by token.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiInvitationsTokenGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserInvitationsApi(config);
            var token = "token_example";  // string | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get an invitation by token.
                UserInvitationDto result = apiInstance.ApiInvitationsTokenGet(token, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsTokenGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiInvitationsTokenGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get an invitation by token.
    ApiResponse<UserInvitationDto> response = apiInstance.ApiInvitationsTokenGetWithHttpInfo(token, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserInvitationsApi.ApiInvitationsTokenGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **token** | **string** |  |  |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**UserInvitationDto**](UserInvitationDto.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

