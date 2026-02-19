# Quater.Desktop.Api.Api.TestResultsApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiTestResultsBySampleSampleIdGet**](TestResultsApi.md#apitestresultsbysamplesampleidget) | **GET** /api/TestResults/by_sample/{sampleId} | Get test results by sample ID with pagination |
| [**ApiTestResultsGet**](TestResultsApi.md#apitestresultsget) | **GET** /api/TestResults | Get all test results with pagination |
| [**ApiTestResultsIdDelete**](TestResultsApi.md#apitestresultsiddelete) | **DELETE** /api/TestResults/{id} | Delete a test result (soft delete) |
| [**ApiTestResultsIdGet**](TestResultsApi.md#apitestresultsidget) | **GET** /api/TestResults/{id} | Get test result by ID |
| [**ApiTestResultsIdPut**](TestResultsApi.md#apitestresultsidput) | **PUT** /api/TestResults/{id} | Update an existing test result |
| [**ApiTestResultsPost**](TestResultsApi.md#apitestresultspost) | **POST** /api/TestResults | Create a new test result |

<a id="apitestresultsbysamplesampleidget"></a>
# **ApiTestResultsBySampleSampleIdGet**
> TestResultDtoPagedResult ApiTestResultsBySampleSampleIdGet (Guid sampleId, int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get test results by sample ID with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiTestResultsBySampleSampleIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new TestResultsApi(config);
            var sampleId = "sampleId_example";  // Guid | 
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get test results by sample ID with pagination
                TestResultDtoPagedResult result = apiInstance.ApiTestResultsBySampleSampleIdGet(sampleId, pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling TestResultsApi.ApiTestResultsBySampleSampleIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiTestResultsBySampleSampleIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get test results by sample ID with pagination
    ApiResponse<TestResultDtoPagedResult> response = apiInstance.ApiTestResultsBySampleSampleIdGetWithHttpInfo(sampleId, pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling TestResultsApi.ApiTestResultsBySampleSampleIdGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **sampleId** | **Guid** |  |  |
| **pageNumber** | **int?** |  | [optional] [default to 1] |
| **pageSize** | **int?** |  | [optional] [default to 50] |
| **apiVersion** | **string?** |  | [optional]  |

### Return type

[**TestResultDtoPagedResult**](TestResultDtoPagedResult.md)

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

<a id="apitestresultsget"></a>
# **ApiTestResultsGet**
> TestResultDtoPagedResult ApiTestResultsGet (int? pageNumber = null, int? pageSize = null, string? apiVersion = null)

Get all test results with pagination

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiTestResultsGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new TestResultsApi(config);
            var pageNumber = 1;  // int? |  (optional)  (default to 1)
            var pageSize = 50;  // int? |  (optional)  (default to 50)
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get all test results with pagination
                TestResultDtoPagedResult result = apiInstance.ApiTestResultsGet(pageNumber, pageSize, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling TestResultsApi.ApiTestResultsGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiTestResultsGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get all test results with pagination
    ApiResponse<TestResultDtoPagedResult> response = apiInstance.ApiTestResultsGetWithHttpInfo(pageNumber, pageSize, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling TestResultsApi.ApiTestResultsGetWithHttpInfo: " + e.Message);
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

[**TestResultDtoPagedResult**](TestResultDtoPagedResult.md)

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

<a id="apitestresultsiddelete"></a>
# **ApiTestResultsIdDelete**
> void ApiTestResultsIdDelete (Guid id, string? apiVersion = null)

Delete a test result (soft delete)

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiTestResultsIdDeleteExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new TestResultsApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Delete a test result (soft delete)
                apiInstance.ApiTestResultsIdDelete(id, apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling TestResultsApi.ApiTestResultsIdDelete: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiTestResultsIdDeleteWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Delete a test result (soft delete)
    apiInstance.ApiTestResultsIdDeleteWithHttpInfo(id, apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling TestResultsApi.ApiTestResultsIdDeleteWithHttpInfo: " + e.Message);
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

<a id="apitestresultsidget"></a>
# **ApiTestResultsIdGet**
> TestResultDto ApiTestResultsIdGet (Guid id, string? apiVersion = null)

Get test result by ID

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiTestResultsIdGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new TestResultsApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Get test result by ID
                TestResultDto result = apiInstance.ApiTestResultsIdGet(id, apiVersion);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling TestResultsApi.ApiTestResultsIdGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiTestResultsIdGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get test result by ID
    ApiResponse<TestResultDto> response = apiInstance.ApiTestResultsIdGetWithHttpInfo(id, apiVersion);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling TestResultsApi.ApiTestResultsIdGetWithHttpInfo: " + e.Message);
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

[**TestResultDto**](TestResultDto.md)

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

<a id="apitestresultsidput"></a>
# **ApiTestResultsIdPut**
> TestResultDto ApiTestResultsIdPut (Guid id, string? apiVersion = null, UpdateTestResultDto? updateTestResultDto = null)

Update an existing test result

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiTestResultsIdPutExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new TestResultsApi(config);
            var id = "id_example";  // Guid | 
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var updateTestResultDto = new UpdateTestResultDto?(); // UpdateTestResultDto? |  (optional) 

            try
            {
                // Update an existing test result
                TestResultDto result = apiInstance.ApiTestResultsIdPut(id, apiVersion, updateTestResultDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling TestResultsApi.ApiTestResultsIdPut: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiTestResultsIdPutWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Update an existing test result
    ApiResponse<TestResultDto> response = apiInstance.ApiTestResultsIdPutWithHttpInfo(id, apiVersion, updateTestResultDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling TestResultsApi.ApiTestResultsIdPutWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **id** | **Guid** |  |  |
| **apiVersion** | **string?** |  | [optional]  |
| **updateTestResultDto** | [**UpdateTestResultDto?**](UpdateTestResultDto?.md) |  | [optional]  |

### Return type

[**TestResultDto**](TestResultDto.md)

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

<a id="apitestresultspost"></a>
# **ApiTestResultsPost**
> TestResultDto ApiTestResultsPost (string? apiVersion = null, CreateTestResultDto? createTestResultDto = null)

Create a new test result

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiTestResultsPostExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new TestResultsApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 
            var createTestResultDto = new CreateTestResultDto?(); // CreateTestResultDto? |  (optional) 

            try
            {
                // Create a new test result
                TestResultDto result = apiInstance.ApiTestResultsPost(apiVersion, createTestResultDto);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling TestResultsApi.ApiTestResultsPost: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiTestResultsPostWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create a new test result
    ApiResponse<TestResultDto> response = apiInstance.ApiTestResultsPostWithHttpInfo(apiVersion, createTestResultDto);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling TestResultsApi.ApiTestResultsPostWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |
| **createTestResultDto** | [**CreateTestResultDto?**](CreateTestResultDto?.md) |  | [optional]  |

### Return type

[**TestResultDto**](TestResultDto.md)

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

