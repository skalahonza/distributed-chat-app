using Grpc.Net.Client;
using System.Net.Http;
using static DSVA.Service.Chat;

namespace DSVA.Lib.Utils
{
    public static class GrpcUtils
    {
        public static ChatClient? Create(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            var httpClientHandler = new HttpClientHandler
            {
                // Return `true` to allow certificates that are untrusted/invalid
                ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            var httpClient = new HttpClient(httpClientHandler);
            var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpClient = httpClient });
            return new ChatClient(channel);
        }
    }
}
