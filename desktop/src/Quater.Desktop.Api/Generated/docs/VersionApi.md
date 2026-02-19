# Quater.Desktop.Api.Api.VersionApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**ApiVersionGet**](VersionApi.md#apiversionget) | **GET** /api/Version | Gets the current API version information. |

<a id="apiversionget"></a>
# **ApiVersionGet**
> void ApiVersionGet (string? apiVersion = null)

Gets the current API version information.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Client;
using Quater.Desktop.Api.Model;

namespace Example
{
    public class ApiVersionGetExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new VersionApi(config);
            var apiVersion = "apiVersion_example";  // string? |  (optional) 

            try
            {
                // Gets the current API version information.
                apiInstance.ApiVersionGet(apiVersion);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling VersionApi.ApiVersionGet: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ApiVersionGetWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Gets the current API version information.
    apiInstance.ApiVersionGetWithHttpInfo(apiVersion);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling VersionApi.ApiVersionGetWithHttpInfo: " + e.Message);
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

