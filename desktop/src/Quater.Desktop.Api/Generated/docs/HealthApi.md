# Quater.Desktop.Api.Api.HealthApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiHealthGet**](HealthApi.md#apihealthget) | **GET** /api/Health | Detailed health check endpoint with comprehensive status information. |
| [**ApiHealthLiveGet**](HealthApi.md#apihealthliveget) | **GET** /api/Health/live | Basic health check endpoint. Returns 200 OK if the service is running. |
| [**ApiHealthReadyGet**](HealthApi.md#apihealthreadyget) | **GET** /api/Health/ready | Readiness check endpoint. Returns 200 OK if the service is ready to accept traffic. Checks database connectivity and other dependencies. |
| [**ApiHealthStartupGet**](HealthApi.md#apihealthstartupget) | **GET** /api/Health/startup | Startup check endpoint. Returns 200 OK if the service has completed startup. |

<a id="apihealthget"></a>
# **ApiHealthGet**
> void ApiHealthGet (string? apiVersion = null)

Detailed health check endpoint with comprehensive status information.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiHealthGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new HealthApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Detailed health check endpoint with comprehensive status information.
                apiInstance.ApiHealthGet(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling HealthApi.ApiHealthGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiHealthGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Detailed health check endpoint with comprehensive status information.
    apiInstance.ApiHealthGetWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling HealthApi.ApiHealthGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **503** | Service Unavailable |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apihealthliveget"></a>
# **ApiHealthLiveGet**
> void ApiHealthLiveGet (string? apiVersion = null)

Basic health check endpoint. Returns 200 OK if the service is running.

Use this endpoint for liveness probes in Kubernetes. It only checks if the application is responsive.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiHealthLiveGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new HealthApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Basic health check endpoint. Returns 200 OK if the service is running.
                apiInstance.ApiHealthLiveGet(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling HealthApi.ApiHealthLiveGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiHealthLiveGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Basic health check endpoint. Returns 200 OK if the service is running.
    apiInstance.ApiHealthLiveGetWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling HealthApi.ApiHealthLiveGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apihealthreadyget"></a>
# **ApiHealthReadyGet**
> void ApiHealthReadyGet (string? apiVersion = null)

Readiness check endpoint. Returns 200 OK if the service is ready to accept traffic. Checks database connectivity and other dependencies.

Use this endpoint for readiness probes in Kubernetes. It verifies that all dependencies are available.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiHealthReadyGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new HealthApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Readiness check endpoint. Returns 200 OK if the service is ready to accept traffic. Checks database connectivity and other dependencies.
                apiInstance.ApiHealthReadyGet(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling HealthApi.ApiHealthReadyGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiHealthReadyGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Readiness check endpoint. Returns 200 OK if the service is ready to accept traffic. Checks database connectivity and other dependencies.
    apiInstance.ApiHealthReadyGetWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling HealthApi.ApiHealthReadyGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |
| **503** | Service Unavailable |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="apihealthstartupget"></a>
# **ApiHealthStartupGet**
> void ApiHealthStartupGet (string? apiVersion = null)

Startup check endpoint. Returns 200 OK if the service has completed startup.

Use this endpoint for startup probes in Kubernetes. It helps with slow-starting containers.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiHealthStartupGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new HealthApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Startup check endpoint. Returns 200 OK if the service has completed startup.
                apiInstance.ApiHealthStartupGet(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling HealthApi.ApiHealthStartupGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiHealthStartupGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Startup check endpoint. Returns 200 OK if the service has completed startup.
    apiInstance.ApiHealthStartupGetWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling HealthApi.ApiHealthStartupGetWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **apiVersion** | **string?** |  | [optional]  |

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: Not defined


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | OK |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

