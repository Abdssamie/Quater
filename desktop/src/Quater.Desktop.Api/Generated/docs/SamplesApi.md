# Quater.Desktop.Api.Api.SamplesApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiSamplesByLabLabIdGet**](SamplesApi.md#apisamplesbylablabidget) | **GET** /api/Samples/by_lab/{labId} | Get samples by lab ID with pagination |
| [**ApiSamplesGet**](SamplesApi.md#apisamplesget) | **GET** /api/Samples | Get all samples with pagination |
| [**ApiSamplesIdDelete**](SamplesApi.md#apisamplesiddelete) | **DELETE** /api/Samples/{id} | Delete a sample (soft delete) |
| [**ApiSamplesIdGet**](SamplesApi.md#apisamplesidget) | **GET** /api/Samples/{id} | Get sample by ID |
| [**ApiSamplesIdPut**](SamplesApi.md#apisamplesidput) | **PUT** /api/Samples/{id} | Update an existing sample |
| [**ApiSamplesPost**](SamplesApi.md#apisamplespost) | **POST** /api/Samples | Create a new sample |

<a id="apisamplesbylablabidget"></a>
# **ApiSamplesByLabLabIdGet**
> SampleDtoPagedResult ApiSamplesByLabLabIdGet (Guid labId, int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get samples by lab ID with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiSamplesByLabLabIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new SamplesApi(config);
            var labId = "labId_example";  // Guid | 
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get samples by lab ID with pagination
                SampleDtoPagedResult result = apiInstance.ApiSamplesByLabLabIdGet(labId, pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SamplesApi.ApiSamplesByLabLabIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiSamplesByLabLabIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get samples by lab ID with pagination
    ApiResponse<SampleDtoPagedResult> response = apiInstance.ApiSamplesByLabLabIdGetWithHttpInfo(labId, pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SamplesApi.ApiSamplesByLabLabIdGetWithHttpInfo: " + e.Message);
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

[**SampleDtoPagedResult**](SampleDtoPagedResult.md)

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

<a id="apisamplesget"></a>
# **ApiSamplesGet**
> SampleDtoPagedResult ApiSamplesGet (int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get all samples with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiSamplesGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new SamplesApi(config);
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get all samples with pagination
                SampleDtoPagedResult result = apiInstance.ApiSamplesGet(pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SamplesApi.ApiSamplesGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiSamplesGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get all samples with pagination
    ApiResponse<SampleDtoPagedResult> response = apiInstance.ApiSamplesGetWithHttpInfo(pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SamplesApi.ApiSamplesGetWithHttpInfo: " + e.Message);
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

[**SampleDtoPagedResult**](SampleDtoPagedResult.md)

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

<a id="apisamplesiddelete"></a>
# **ApiSamplesIdDelete**
> void ApiSamplesIdDelete (Guid id, string? apiVersion = null)

Delete a sample (soft delete)

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiSamplesIdDeleteExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new SamplesApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Delete a sample (soft delete)
                apiInstance.ApiSamplesIdDelete(id, apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SamplesApi.ApiSamplesIdDelete: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiSamplesIdDeleteWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete a sample (soft delete)
    apiInstance.ApiSamplesIdDeleteWithHttpInfo(id, apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SamplesApi.ApiSamplesIdDeleteWithHttpInfo: " + e.Message);
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

<a id="apisamplesidget"></a>
# **ApiSamplesIdGet**
> SampleDto ApiSamplesIdGet (Guid id, string? apiVersion = null)

Get sample by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiSamplesIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new SamplesApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get sample by ID
                SampleDto result = apiInstance.ApiSamplesIdGet(id, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SamplesApi.ApiSamplesIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiSamplesIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get sample by ID
    ApiResponse<SampleDto> response = apiInstance.ApiSamplesIdGetWithHttpInfo(id, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SamplesApi.ApiSamplesIdGetWithHttpInfo: " + e.Message);
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

[**SampleDto**](SampleDto.md)

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

<a id="apisamplesidput"></a>
# **ApiSamplesIdPut**
> SampleDto ApiSamplesIdPut (Guid id, string? apiVersion = null, UpdateSampleDto? updateSampleDto = null)

Update an existing sample

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiSamplesIdPutExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new SamplesApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var updateSampleDto = new UpdateSampleDto?(); // UpdateSampleDto? |  (optional) 

            try
            {
                // Update an existing sample
                SampleDto result = apiInstance.ApiSamplesIdPut(id, apiVersion, updateSampleDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SamplesApi.ApiSamplesIdPut: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiSamplesIdPutWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update an existing sample
    ApiResponse<SampleDto> response = apiInstance.ApiSamplesIdPutWithHttpInfo(id, apiVersion, updateSampleDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SamplesApi.ApiSamplesIdPutWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |
| **updateSampleDto** | [**UpdateSampleDto?**](UpdateSampleDto?.md) |  | [optional]  |

### Return type

[**SampleDto**](SampleDto.md)

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

<a id="apisamplespost"></a>
# **ApiSamplesPost**
> SampleDto ApiSamplesPost (string? apiVersion = null, CreateSampleDto? createSampleDto = null)

Create a new sample

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiSamplesPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new SamplesApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var createSampleDto = new CreateSampleDto?(); // CreateSampleDto? |  (optional) 

            try
            {
                // Create a new sample
                SampleDto result = apiInstance.ApiSamplesPost(apiVersion, createSampleDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling SamplesApi.ApiSamplesPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiSamplesPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create a new sample
    ApiResponse<SampleDto> response = apiInstance.ApiSamplesPostWithHttpInfo(apiVersion, createSampleDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling SamplesApi.ApiSamplesPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **createSampleDto** | [**CreateSampleDto?**](CreateSampleDto?.md) |  | [optional]  |

### Return type

[**SampleDto**](SampleDto.md)

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

