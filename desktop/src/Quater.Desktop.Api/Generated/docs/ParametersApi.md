# Quater.Desktop.Api.Api.ParametersApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiParametersActiveGet**](ParametersApi.md#apiparametersactiveget) | **GET** /api/Parameters/active | Get active parameters only |
| [**ApiParametersGet**](ParametersApi.md#apiparametersget) | **GET** /api/Parameters | Get all parameters with pagination |
| [**ApiParametersIdDelete**](ParametersApi.md#apiparametersiddelete) | **DELETE** /api/Parameters/{id} | Delete a parameter (soft delete - marks as inactive) |
| [**ApiParametersIdGet**](ParametersApi.md#apiparametersidget) | **GET** /api/Parameters/{id} | Get parameter by ID |
| [**ApiParametersIdPut**](ParametersApi.md#apiparametersidput) | **PUT** /api/Parameters/{id} | Update an existing parameter |
| [**ApiParametersPost**](ParametersApi.md#apiparameterspost) | **POST** /api/Parameters | Create a new parameter |

<a id="apiparametersactiveget"></a>
# **ApiParametersActiveGet**
> List&lt;ParameterDto&gt; ApiParametersActiveGet (string? apiVersion = null)

Get active parameters only

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiParametersActiveGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new ParametersApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get active parameters only
                List<ParameterDto> result = apiInstance.ApiParametersActiveGet(apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ParametersApi.ApiParametersActiveGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiParametersActiveGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get active parameters only
    ApiResponse<List<ParameterDto>> response = apiInstance.ApiParametersActiveGetWithHttpInfo(apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ParametersApi.ApiParametersActiveGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**List&lt;ParameterDto&gt;**](ParameterDto.md)

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

<a id="apiparametersget"></a>
# **ApiParametersGet**
> ParameterDtoPagedResult ApiParametersGet (int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get all parameters with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiParametersGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new ParametersApi(config);
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get all parameters with pagination
                ParameterDtoPagedResult result = apiInstance.ApiParametersGet(pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ParametersApi.ApiParametersGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiParametersGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get all parameters with pagination
    ApiResponse<ParameterDtoPagedResult> response = apiInstance.ApiParametersGetWithHttpInfo(pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ParametersApi.ApiParametersGetWithHttpInfo: " + e.Message);
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

[**ParameterDtoPagedResult**](ParameterDtoPagedResult.md)

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

<a id="apiparametersiddelete"></a>
# **ApiParametersIdDelete**
> void ApiParametersIdDelete (Guid id, string? apiVersion = null)

Delete a parameter (soft delete - marks as inactive)

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiParametersIdDeleteExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new ParametersApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Delete a parameter (soft delete - marks as inactive)
                apiInstance.ApiParametersIdDelete(id, apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ParametersApi.ApiParametersIdDelete: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiParametersIdDeleteWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete a parameter (soft delete - marks as inactive)
    apiInstance.ApiParametersIdDeleteWithHttpInfo(id, apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ParametersApi.ApiParametersIdDeleteWithHttpInfo: " + e.Message);
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

<a id="apiparametersidget"></a>
# **ApiParametersIdGet**
> ParameterDto ApiParametersIdGet (Guid id, string? apiVersion = null)

Get parameter by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiParametersIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new ParametersApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get parameter by ID
                ParameterDto result = apiInstance.ApiParametersIdGet(id, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ParametersApi.ApiParametersIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiParametersIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get parameter by ID
    ApiResponse<ParameterDto> response = apiInstance.ApiParametersIdGetWithHttpInfo(id, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ParametersApi.ApiParametersIdGetWithHttpInfo: " + e.Message);
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

[**ParameterDto**](ParameterDto.md)

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

<a id="apiparametersidput"></a>
# **ApiParametersIdPut**
> ParameterDto ApiParametersIdPut (Guid id, string? apiVersion = null, UpdateParameterDto? updateParameterDto = null)

Update an existing parameter

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiParametersIdPutExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new ParametersApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var updateParameterDto = new UpdateParameterDto?(); // UpdateParameterDto? |  (optional) 

            try
            {
                // Update an existing parameter
                ParameterDto result = apiInstance.ApiParametersIdPut(id, apiVersion, updateParameterDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ParametersApi.ApiParametersIdPut: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiParametersIdPutWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update an existing parameter
    ApiResponse<ParameterDto> response = apiInstance.ApiParametersIdPutWithHttpInfo(id, apiVersion, updateParameterDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ParametersApi.ApiParametersIdPutWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |
| **updateParameterDto** | [**UpdateParameterDto?**](UpdateParameterDto?.md) |  | [optional]  |

### Return type

[**ParameterDto**](ParameterDto.md)

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

<a id="apiparameterspost"></a>
# **ApiParametersPost**
> ParameterDto ApiParametersPost (string? apiVersion = null, CreateParameterDto? createParameterDto = null)

Create a new parameter

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiParametersPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new ParametersApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var createParameterDto = new CreateParameterDto?(); // CreateParameterDto? |  (optional) 

            try
            {
                // Create a new parameter
                ParameterDto result = apiInstance.ApiParametersPost(apiVersion, createParameterDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ParametersApi.ApiParametersPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiParametersPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create a new parameter
    ApiResponse<ParameterDto> response = apiInstance.ApiParametersPostWithHttpInfo(apiVersion, createParameterDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ParametersApi.ApiParametersPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **createParameterDto** | [**CreateParameterDto?**](CreateParameterDto?.md) |  | [optional]  |

### Return type

[**ParameterDto**](ParameterDto.md)

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

