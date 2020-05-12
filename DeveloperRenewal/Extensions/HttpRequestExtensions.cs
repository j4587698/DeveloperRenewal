using System.Text;
using Microsoft.AspNetCore.Http;

namespace DeveloperRenewal.Extensions
{
    public static class HttpRequestExtensions
    {
        public static string GetAbsoluteUri(this HttpRequest request)
        {
            return new StringBuilder()
                .Append(request.Scheme)
                .Append("://")
                .Append(request.Host)
                .Append(request.PathBase)
                .Append(request.Path)
                .Append(request.QueryString)
                .ToString();
        }

        public static string GetHostUri(this HttpRequest request)
        {
            return new StringBuilder()
                .Append(request.Scheme)
                .Append("://")
                .Append(request.Host)
                .ToString();
        }

        public static string GetRegisterUrl(this HttpRequest request)
        {
            return GetHostUri(request) + "/Home/Register";
        }
    }
}