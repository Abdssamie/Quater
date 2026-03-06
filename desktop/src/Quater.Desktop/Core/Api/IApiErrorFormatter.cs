using Quater.Desktop.Api.Client;

namespace Quater.Desktop.Core.Api;

public interface IApiErrorFormatter
{
    string Format(ApiException exception, string fallbackMessage);
}
