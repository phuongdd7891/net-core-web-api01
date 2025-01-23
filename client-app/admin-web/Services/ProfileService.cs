using AdminWeb.Models.Response;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace AdminWeb.Services
{
    public class ProfileService: BaseService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ITempDataDictionary _tempDataDictionary;

        public ProfileService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor contextAccessor
        ) : base(httpClient, configuration, contextAccessor)
        {
            _contextAccessor = contextAccessor;
            var tempDataDictionaryFactory = _contextAccessor.HttpContext!.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            _tempDataDictionary = tempDataDictionaryFactory.GetTempData(_contextAccessor.HttpContext);
        }

        public async Task LoadProfile()
        {
            var token = _contextAccessor.HttpContext!.User.Claims.FirstOrDefault(a => a.Type == "Token")!.Value;
            var username = _contextAccessor.HttpContext!.User.Identity!.Name;
            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + token }
            };
            var result = await GetAsync<ApiResponse<AdminProfile>>($"gw-api/admin/user-profile?u={username}", headers);
            var profile = result.Data;
            var tempProfile = JsonConvert.SerializeObject(result.Data);
            if (_tempDataDictionary.ContainsKey("Profile"))
            {
                _tempDataDictionary.Remove("Profile");
            }
            _tempDataDictionary["Profile"] = tempProfile;
            _tempDataDictionary.Save();
        }

        public AdminProfile? GetProfile()
        {
            var userData = _tempDataDictionary["Profile"] as string;
            if (!string.IsNullOrEmpty(userData))
            {
                _tempDataDictionary.Keep("Profile");
                var profile = JsonConvert.DeserializeObject<AdminProfile>(userData);
                return profile;
            }
            return null;
        }
    }
}
