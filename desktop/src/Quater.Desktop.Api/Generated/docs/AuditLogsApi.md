# Quater.Desktop.Api.Api.AuditLogsApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiAuditLogsByEntityEntityIdGet**](AuditLogsApi.md#apiauditlogsbyentityentityidget) | **GET** /api/AuditLogs/by_entity/{entityId} | Get audit logs by entity ID |
| [**ApiAuditLogsByUserUserIdGet**](AuditLogsApi.md#apiauditlogsbyuseruseridget) | **GET** /api/AuditLogs/by_user/{userId} | Get audit logs by user ID |
| [**ApiAuditLogsFilterPost**](AuditLogsApi.md#apiauditlogsfilterpost) | **POST** /api/AuditLogs/filter | Get audit logs with advanced filtering |
| [**ApiAuditLogsGet**](AuditLogsApi.md#apiauditlogsget) | **GET** /api/AuditLogs | Get all audit logs with pagination |
| [**ApiAuditLogsIdGet**](AuditLogsApi.md#apiauditlogsidget) | **GET** /api/AuditLogs/{id} | Get audit log by ID |

<a id="apiauditlogsbyentityentityidget"></a>
# **ApiAuditLogsByEntityEntityIdGet**
> AuditLogDtoPagedResult ApiAuditLogsByEntityEntityIdGet (Guid entityId, int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get audit logs by entity ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuditLogsByEntityEntityIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuditLogsApi(config);
            var entityId = "entityId_example";  // Guid | 
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get audit logs by entity ID
                AuditLogDtoPagedResult result = apiInstance.ApiAuditLogsByEntityEntityIdGet(entityId, pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsByEntityEntityIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuditLogsByEntityEntityIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get audit logs by entity ID
    ApiResponse<AuditLogDtoPagedResult> response = apiInstance.ApiAuditLogsByEntityEntityIdGetWithHttpInfo(entityId, pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsByEntityEntityIdGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **entityId** | **Guid** |  |  |
| **pageNumber** | **int?** |  | [optional] [default to 1] |
| **pageSize** | **int?** |  | [optional] [default to 50] |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**AuditLogDtoPagedResult**](AuditLogDtoPagedResult.md)

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

<a id="apiauditlogsbyuseruseridget"></a>
# **ApiAuditLogsByUserUserIdGet**
> AuditLogDtoPagedResult ApiAuditLogsByUserUserIdGet (Guid userId, int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get audit logs by user ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuditLogsByUserUserIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuditLogsApi(config);
            var userId = "userId_example";  // Guid | 
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get audit logs by user ID
                AuditLogDtoPagedResult result = apiInstance.ApiAuditLogsByUserUserIdGet(userId, pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsByUserUserIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuditLogsByUserUserIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get audit logs by user ID
    ApiResponse<AuditLogDtoPagedResult> response = apiInstance.ApiAuditLogsByUserUserIdGetWithHttpInfo(userId, pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsByUserUserIdGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **userId** | **Guid** |  |  |
| **pageNumber** | **int?** |  | [optional] [default to 1] |
| **pageSize** | **int?** |  | [optional] [default to 50] |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**AuditLogDtoPagedResult**](AuditLogDtoPagedResult.md)

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

<a id="apiauditlogsfilterpost"></a>
# **ApiAuditLogsFilterPost**
> AuditLogDtoPagedResult ApiAuditLogsFilterPost (string? apiVersion = null, AuditLogFilterDto? auditLogFilterDto = null)

Get audit logs with advanced filtering

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuditLogsFilterPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuditLogsApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var auditLogFilterDto = new AuditLogFilterDto?(); // AuditLogFilterDto? |  (optional) 

            try
            {
                // Get audit logs with advanced filtering
                AuditLogDtoPagedResult result = apiInstance.ApiAuditLogsFilterPost(apiVersion, auditLogFilterDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsFilterPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuditLogsFilterPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get audit logs with advanced filtering
    ApiResponse<AuditLogDtoPagedResult> response = apiInstance.ApiAuditLogsFilterPostWithHttpInfo(apiVersion, auditLogFilterDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsFilterPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **auditLogFilterDto** | [**AuditLogFilterDto?**](AuditLogFilterDto?.md) |  | [optional]  |

### Return type

[**AuditLogDtoPagedResult**](AuditLogDtoPagedResult.md)

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

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apiauditlogsget"></a>
# **ApiAuditLogsGet**
> AuditLogDtoPagedResult ApiAuditLogsGet (int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get all audit logs with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuditLogsGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuditLogsApi(config);
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get all audit logs with pagination
                AuditLogDtoPagedResult result = apiInstance.ApiAuditLogsGet(pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuditLogsGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get all audit logs with pagination
    ApiResponse<AuditLogDtoPagedResult> response = apiInstance.ApiAuditLogsGetWithHttpInfo(pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsGetWithHttpInfo: " + e.Message);
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

[**AuditLogDtoPagedResult**](AuditLogDtoPagedResult.md)

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

<a id="apiauditlogsidget"></a>
# **ApiAuditLogsIdGet**
> AuditLogDto ApiAuditLogsIdGet (Guid id, string? apiVersion = null)

Get audit log by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiAuditLogsIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new AuditLogsApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get audit log by ID
                AuditLogDto result = apiInstance.ApiAuditLogsIdGet(id, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiAuditLogsIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get audit log by ID
    ApiResponse<AuditLogDto> response = apiInstance.ApiAuditLogsIdGetWithHttpInfo(id, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling AuditLogsApi.ApiAuditLogsIdGetWithHttpInfo: " + e.Message);
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

[**AuditLogDto**](AuditLogDto.md)

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

