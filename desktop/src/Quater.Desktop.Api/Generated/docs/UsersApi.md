# Quater.Desktop.Api.Api.UsersApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiUsersActiveGet**](UsersApi.md#apiusersactiveget) | **GET** /api/Users/active | Get active users only (non-paginated, useful for dropdowns) |
| [**ApiUsersByLabLabIdGet**](UsersApi.md#apiusersbylablabidget) | **GET** /api/Users/by_lab/{labId} | Get users by lab ID with pagination |
| [**ApiUsersGet**](UsersApi.md#apiusersget) | **GET** /api/Users | Get all users with pagination |
| [**ApiUsersIdDelete**](UsersApi.md#apiusersiddelete) | **DELETE** /api/Users/{id} | Delete a user (soft delete - marks as inactive) |
| [**ApiUsersIdGet**](UsersApi.md#apiusersidget) | **GET** /api/Users/{id} | Get user by ID |
| [**ApiUsersIdPut**](UsersApi.md#apiusersidput) | **PUT** /api/Users/{id} | Update an existing user |
| [**ApiUsersPost**](UsersApi.md#apiuserspost) | **POST** /api/Users | Create a new user |

<a id="apiusersactiveget"></a>
# **ApiUsersActiveGet**
> List&lt;UserDto&gt; ApiUsersActiveGet (string? apiVersion = null)

Get active users only (non-paginated, useful for dropdowns)

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersActiveGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UsersApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get active users only (non-paginated, useful for dropdowns)
                List<UserDto> result = apiInstance.ApiUsersActiveGet(apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UsersApi.ApiUsersActiveGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersActiveGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get active users only (non-paginated, useful for dropdowns)
    ApiResponse<List<UserDto>> response = apiInstance.ApiUsersActiveGetWithHttpInfo(apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UsersApi.ApiUsersActiveGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**List&lt;UserDto&gt;**](UserDto.md)

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

<a id="apiusersbylablabidget"></a>
# **ApiUsersByLabLabIdGet**
> UserDtoPagedResult ApiUsersByLabLabIdGet (Guid labId, int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get users by lab ID with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersByLabLabIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UsersApi(config);
            var labId = "labId_example";  // Guid | 
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get users by lab ID with pagination
                UserDtoPagedResult result = apiInstance.ApiUsersByLabLabIdGet(labId, pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UsersApi.ApiUsersByLabLabIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersByLabLabIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get users by lab ID with pagination
    ApiResponse<UserDtoPagedResult> response = apiInstance.ApiUsersByLabLabIdGetWithHttpInfo(labId, pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UsersApi.ApiUsersByLabLabIdGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **labId** | **Guid** |  |  |
| **pageNumber** | **int?** |  | [optional] [default to 1] |
| **pageSize** | **int?** |  | [optional] [default to 50] |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**UserDtoPagedResult**](UserDtoPagedResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiusersget"></a>
# **ApiUsersGet**
> UserDtoPagedResult ApiUsersGet (int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get all users with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UsersApi(config);
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get all users with pagination
                UserDtoPagedResult result = apiInstance.ApiUsersGet(pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UsersApi.ApiUsersGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get all users with pagination
    ApiResponse<UserDtoPagedResult> response = apiInstance.ApiUsersGetWithHttpInfo(pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UsersApi.ApiUsersGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **pageNumber** | **int?** |  | [optional] [default to 1] |
| **pageSize** | **int?** |  | [optional] [default to 50] |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**UserDtoPagedResult**](UserDtoPagedResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **400** | Bad Request |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiusersiddelete"></a>
# **ApiUsersIdDelete**
> void ApiUsersIdDelete (Guid id, string? apiVersion = null)

Delete a user (soft delete - marks as inactive)

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersIdDeleteExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UsersApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Delete a user (soft delete - marks as inactive)
                apiInstance.ApiUsersIdDelete(id, apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UsersApi.ApiUsersIdDelete: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersIdDeleteWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete a user (soft delete - marks as inactive)
    apiInstance.ApiUsersIdDeleteWithHttpInfo(id, apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UsersApi.ApiUsersIdDeleteWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **Guid** |  |  |
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

<a id="apiusersidget"></a>
# **ApiUsersIdGet**
> UserDto ApiUsersIdGet (Guid id, string? apiVersion = null)

Get user by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UsersApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get user by ID
                UserDto result = apiInstance.ApiUsersIdGet(id, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UsersApi.ApiUsersIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get user by ID
    ApiResponse<UserDto> response = apiInstance.ApiUsersIdGetWithHttpInfo(id, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UsersApi.ApiUsersIdGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **Guid** |  |  |
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
| **404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiusersidput"></a>
# **ApiUsersIdPut**
> UserDto ApiUsersIdPut (Guid id, string? apiVersion = null, UpdateUserDto? updateUserDto = null)

Update an existing user

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersIdPutExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UsersApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var updateUserDto = new UpdateUserDto?(); // UpdateUserDto? |  (optional) 

            try
            {
                // Update an existing user
                UserDto result = apiInstance.ApiUsersIdPut(id, apiVersion, updateUserDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UsersApi.ApiUsersIdPut: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersIdPutWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update an existing user
    ApiResponse<UserDto> response = apiInstance.ApiUsersIdPutWithHttpInfo(id, apiVersion, updateUserDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UsersApi.ApiUsersIdPutWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |
| **updateUserDto** | [**UpdateUserDto?**](UpdateUserDto?.md) |  | [optional]  |

### Return type

[**UserDto**](UserDto.md)

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
| **409** | Conflict |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiuserspost"></a>
# **ApiUsersPost**
> UserDto ApiUsersPost (string? apiVersion = null, CreateUserDto? createUserDto = null)

Create a new user

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiUsersPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new UsersApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var createUserDto = new CreateUserDto?(); // CreateUserDto? |  (optional) 

            try
            {
                // Create a new user
                UserDto result = apiInstance.ApiUsersPost(apiVersion, createUserDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling UsersApi.ApiUsersPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiUsersPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create a new user
    ApiResponse<UserDto> response = apiInstance.ApiUsersPostWithHttpInfo(apiVersion, createUserDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling UsersApi.ApiUsersPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **createUserDto** | [**CreateUserDto?**](CreateUserDto?.md) |  | [optional]  |

### Return type

[**UserDto**](UserDto.md)

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
| **404** | Not Found |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

