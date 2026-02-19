# Quater.Desktop.Api.Api.UserLabsApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiUsersUserIdLabsGet**](UserLabsApi.md#apiusersuseridlabsget) | **GET** /api/users/{userId}/labs | Gets all labs a user belongs to (placeholder for consistency). |
| [**ApiUsersUserIdLabsLabIdDelete**](UserLabsApi.md#apiusersuseridlabslabiddelete) | **DELETE** /api/users/{userId}/labs/{labId} | Removes a user from a lab. |
| [**ApiUsersUserIdLabsLabIdPost**](UserLabsApi.md#apiusersuseridlabslabidpost) | **POST** /api/users/{userId}/labs/{labId} | Adds a user to a lab with the specified role. |
| [**ApiUsersUserIdLabsLabIdRolePut**](UserLabsApi.md#apiusersuseridlabslabidroleput) | **PUT** /api/users/{userId}/labs/{labId}/role | Updates a user&#39;s role in a specific lab. |

<a id="apiusersuseridlabsget"></a>
# **ApiUsersUserIdLabsGet**
> List&lt;UserLabDto&gt; ApiUsersUserIdLabsGet (Guid userId, string? apiVersion = null)

Gets all labs a user belongs to (placeholder for consistency).

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersUserIdLabsGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserLabsApi(config);
            var userId = "userId_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Gets all labs a user belongs to (placeholder for consistency).
                List<UserLabDto> result = apiInstance.ApiUsersUserIdLabsGet(userId, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserLabsApi.ApiUsersUserIdLabsGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersUserIdLabsGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Gets all labs a user belongs to (placeholder for consistency).
    ApiResponse<List<UserLabDto>> response = apiInstance.ApiUsersUserIdLabsGetWithHttpInfo(userId, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserLabsApi.ApiUsersUserIdLabsGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **userId** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**List&lt;UserLabDto&gt;**](UserLabDto.md)

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

<a id="apiusersuseridlabslabiddelete"></a>
# **ApiUsersUserIdLabsLabIdDelete**
> void ApiUsersUserIdLabsLabIdDelete (Guid userId, Guid labId, string? apiVersion = null)

Removes a user from a lab.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersUserIdLabsLabIdDeleteExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserLabsApi(config);
            var userId = "userId_example";  // Guid | 
            var labId = "labId_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Removes a user from a lab.
                apiInstance.ApiUsersUserIdLabsLabIdDelete(userId, labId, apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserLabsApi.ApiUsersUserIdLabsLabIdDelete: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersUserIdLabsLabIdDeleteWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Removes a user from a lab.
    apiInstance.ApiUsersUserIdLabsLabIdDeleteWithHttpInfo(userId, labId, apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserLabsApi.ApiUsersUserIdLabsLabIdDeleteWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **userId** | **Guid** |  |  |
| **labId** | **Guid** |  |  |
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

<a id="apiusersuseridlabslabidpost"></a>
# **ApiUsersUserIdLabsLabIdPost**
> UserLabDto ApiUsersUserIdLabsLabIdPost (Guid userId, Guid labId, string? apiVersion = null, AddUserToLabRequest? addUserToLabRequest = null)

Adds a user to a lab with the specified role.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersUserIdLabsLabIdPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserLabsApi(config);
            var userId = "userId_example";  // Guid | 
            var labId = "labId_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var addUserToLabRequest = new AddUserToLabRequest?(); // AddUserToLabRequest? |  (optional) 

            try
            {
                // Adds a user to a lab with the specified role.
                UserLabDto result = apiInstance.ApiUsersUserIdLabsLabIdPost(userId, labId, apiVersion, addUserToLabRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserLabsApi.ApiUsersUserIdLabsLabIdPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersUserIdLabsLabIdPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Adds a user to a lab with the specified role.
    ApiResponse<UserLabDto> response = apiInstance.ApiUsersUserIdLabsLabIdPostWithHttpInfo(userId, labId, apiVersion, addUserToLabRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserLabsApi.ApiUsersUserIdLabsLabIdPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **userId** | **Guid** |  |  |
| **labId** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |
| **addUserToLabRequest** | [**AddUserToLabRequest?**](AddUserToLabRequest?.md) |  | [optional]  |

### Return type

[**UserLabDto**](UserLabDto.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Created |  -  |
| **404** | Not Found |  -  |
| **409** | Conflict |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiusersuseridlabslabidroleput"></a>
# **ApiUsersUserIdLabsLabIdRolePut**
> UserLabDto ApiUsersUserIdLabsLabIdRolePut (Guid userId, Guid labId, string? apiVersion = null, UpdateUserRoleRequest? updateUserRoleRequest = null)

Updates a user's role in a specific lab.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersUserIdLabsLabIdRolePutExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UserLabsApi(config);
            var userId = "userId_example";  // Guid | 
            var labId = "labId_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var updateUserRoleRequest = new UpdateUserRoleRequest?(); // UpdateUserRoleRequest? |  (optional) 

            try
            {
                // Updates a user's role in a specific lab.
                UserLabDto result = apiInstance.ApiUsersUserIdLabsLabIdRolePut(userId, labId, apiVersion, updateUserRoleRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UserLabsApi.ApiUsersUserIdLabsLabIdRolePut: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersUserIdLabsLabIdRolePutWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Updates a user's role in a specific lab.
    ApiResponse<UserLabDto> response = apiInstance.ApiUsersUserIdLabsLabIdRolePutWithHttpInfo(userId, labId, apiVersion, updateUserRoleRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UserLabsApi.ApiUsersUserIdLabsLabIdRolePutWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **userId** | **Guid** |  |  |
| **labId** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |
| **updateUserRoleRequest** | [**UpdateUserRoleRequest?**](UpdateUserRoleRequest?.md) |  | [optional]  |

### Return type

[**UserLabDto**](UserLabDto.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

