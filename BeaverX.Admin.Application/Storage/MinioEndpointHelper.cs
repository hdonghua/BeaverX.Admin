namespace BeaverX.Admin.Application.Storage;

internal static class MinioEndpointHelper
{
    public static (string Endpoint, bool UseSsl) Parse(string endpoint, bool configuredUseSsl)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            var hostEndpoint = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            var useSsl = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) || configuredUseSsl;
            return (hostEndpoint, useSsl);
        }

        return (endpoint.Trim(), configuredUseSsl);
    }
}
