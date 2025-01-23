using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using AdminWeb.Models.Response;
using System.Net;
using System.Net.Http.Headers;
using System.Linq;

namespace AdminWeb.Services
{
    public class BaseService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string baseApiAddress;

        public BaseService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            baseApiAddress = _configuration.GetValue<string>("BaseApiUrl") ?? throw new ArgumentNullException("BaseApiUrl", "BaseApiUrl configuration is missing.");
            _httpClient.BaseAddress = new Uri(baseApiAddress);

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<TResponse> SendHttpRequest<TRequest, TResponse>(string url, HttpMethod httpMethod, TRequest? requestBody = default, Dictionary<string, string>? headers = null)
        {
            var request = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri($"{baseApiAddress}{url}"),
            };
            if (headers != null)
            {
                foreach (var item in headers)
                {
                    request.Headers.Add(item.Key, item.Value);
                }
            }
            if (requestBody != null)
            {
                var json = JsonConvert.SerializeObject(requestBody);
                request.Content = new StringContent(json, Encoding.UTF8, Application.Json);
            }
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var context = _httpContextAccessor.HttpContext;
                    if (context != null)
                    {
                        throw new RedirectException("/home/error?redirectUrl=/account");
                    }
                }
                var responseErrData = JsonConvert.DeserializeObject<ApiResponse<string>>(responseJson);
                if (!string.IsNullOrEmpty(responseErrData?.Data))
                {
                    responseErrData.Data = responseErrData.Data.Replace("'", "\"");
                }
                throw new HttpRequestException($"{responseErrData.Code}", new Exception(responseErrData.Data), response.StatusCode);
            }
            var responseData = JsonConvert.DeserializeObject<TResponse>(responseJson);
            return responseData;
        }

        public Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest? requestBody)
        {
            return SendHttpRequest<TRequest, TResponse>(endpoint, HttpMethod.Post, requestBody);
        }

        public Task<TResponse> GetAsync<TResponse>(string endpoint)
        {
            return SendHttpRequest<Object, TResponse>(endpoint, HttpMethod.Get);
        }

        public Task<TResponse> GetAsync<TResponse>(string endpoint, Dictionary<string, string> headers)
        {
            return SendHttpRequest<Object, TResponse>(endpoint, HttpMethod.Get, null, headers);
        }
    }

    public class RedirectException : Exception
    {
        public string RedirectUrl { get; }
        public bool IsPermanent { get; }

        public RedirectException(string redirectUrl, bool isPermanent = false)
        {
            RedirectUrl = redirectUrl;
            IsPermanent = isPermanent;
        }
    }

}
