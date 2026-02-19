# Quater.Desktop.Api.Api.LabsApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiLabsActiveGet**](LabsApi.md#apilabsactiveget) | **GET** /api/Labs/active | Get active labs only |
| [**ApiLabsGet**](LabsApi.md#apilabsget) | **GET** /api/Labs | Get all labs with pagination |
| [**ApiLabsIdDelete**](LabsApi.md#apilabsiddelete) | **DELETE** /api/Labs/{id} | Delete a lab (soft delete - marks as inactive) |
| [**ApiLabsIdGet**](LabsApi.md#apilabsidget) | **GET** /api/Labs/{id} | Get lab by ID |
| [**ApiLabsIdPut**](LabsApi.md#apilabsidput) | **PUT** /api/Labs/{id} | Update an existing lab |
| [**ApiLabsPost**](LabsApi.md#apilabspost) | **POST** /api/Labs | Create a new lab |

<a id="apilabsactiveget"></a>
# **ApiLabsActiveGet**
> List&lt;LabDto&gt; ApiLabsActiveGet (string? apiVersion = null)

Get active labs only

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiLabsActiveGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new LabsApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get active labs only
                List<LabDto> result = apiInstance.ApiLabsActiveGet(apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling LabsApi.ApiLabsActiveGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiLabsActiveGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get active labs only
    ApiResponse<List<LabDto>> response = apiInstance.ApiLabsActiveGetWithHttpInfo(apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling LabsApi.ApiLabsActiveGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**List&lt;LabDto&gt;**](LabDto.md)

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

<a id="apilabsget"></a>
# **ApiLabsGet**
> LabDtoPagedResult ApiLabsGet (int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get all labs with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiLabsGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new LabsApi(config);
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get all labs with pagination
                LabDtoPagedResult result = apiInstance.ApiLabsGet(pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling LabsApi.ApiLabsGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiLabsGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get all labs with pagination
    ApiResponse<LabDtoPagedResult> response = apiInstance.ApiLabsGetWithHttpInfo(pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling LabsApi.ApiLabsGetWithHttpInfo: " + e.Message);
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

[**LabDtoPagedResult**](LabDtoPagedResult.md)

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

<a id="apilabsiddelete"></a>
# **ApiLabsIdDelete**
> void ApiLabsIdDelete (Guid id, string? apiVersion = null)

Delete a lab (soft delete - marks as inactive)

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiLabsIdDeleteExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new LabsApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Delete a lab (soft delete - marks as inactive)
                apiInstance.ApiLabsIdDelete(id, apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling LabsApi.ApiLabsIdDelete: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiLabsIdDeleteWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete a lab (soft delete - marks as inactive)
    apiInstance.ApiLabsIdDeleteWithHttpInfo(id, apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling LabsApi.ApiLabsIdDeleteWithHttpInfo: " + e.Message);
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

<a id="apilabsidget"></a>
# **ApiLabsIdGet**
> LabDto ApiLabsIdGet (Guid id, string? apiVersion = null)

Get lab by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiLabsIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new LabsApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get lab by ID
                LabDto result = apiInstance.ApiLabsIdGet(id, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling LabsApi.ApiLabsIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiLabsIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get lab by ID
    ApiResponse<LabDto> response = apiInstance.ApiLabsIdGetWithHttpInfo(id, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling LabsApi.ApiLabsIdGetWithHttpInfo: " + e.Message);
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

[**LabDto**](LabDto.md)

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

<a id="apilabsidput"></a>
# **ApiLabsIdPut**
> LabDto ApiLabsIdPut (Guid id, string? apiVersion = null, UpdateLabDto? updateLabDto = null)

Update an existing lab

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiLabsIdPutExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new LabsApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var updateLabDto = new UpdateLabDto?(); // UpdateLabDto? |  (optional) 

            try
            {
                // Update an existing lab
                LabDto result = apiInstance.ApiLabsIdPut(id, apiVersion, updateLabDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling LabsApi.ApiLabsIdPut: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiLabsIdPutWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update an existing lab
    ApiResponse<LabDto> response = apiInstance.ApiLabsIdPutWithHttpInfo(id, apiVersion, updateLabDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling LabsApi.ApiLabsIdPutWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |
| **updateLabDto** | [**UpdateLabDto?**](UpdateLabDto?.md) |  | [optional]  |

### Return type

[**LabDto**](LabDto.md)

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

<a id="apilabspost"></a>
# **ApiLabsPost**
> LabDto ApiLabsPost (string? apiVersion = null, CreateLabDto? createLabDto = null)

Create a new lab

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiLabsPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new LabsApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var createLabDto = new CreateLabDto?(); // CreateLabDto? |  (optional) 

            try
            {
                // Create a new lab
                LabDto result = apiInstance.ApiLabsPost(apiVersion, createLabDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling LabsApi.ApiLabsPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiLabsPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create a new lab
    ApiResponse<LabDto> response = apiInstance.ApiLabsPostWithHttpInfo(apiVersion, createLabDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling LabsApi.ApiLabsPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **createLabDto** | [**CreateLabDto?**](CreateLabDto?.md) |  | [optional]  |

### Return type

[**LabDto**](LabDto.md)

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

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

