using AspNetCoreHero.ToastNotification.Abstractions;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace AdminWeb.Services
{
    public class ToastMessageService
    {
        private readonly string Key = "AspNetCoreHero.ToastNotification";
        private readonly ITempDataService _tempDataService;
        private readonly Dictionary<string, string> _messageDictionary;

        public ToastMessageService(
            ITempDataService tempDataService
        )
        {
            _tempDataService = tempDataService;
            _messageDictionary = new Dictionary<string, string>()
            {
                {Const.ErrCode_InvalidToken, "Invalid token"}
            };

        }

        public void AddError(string message, string? code = null)
        {
            _tempDataService.Add(Key, new object[] {
                new {
                    Type = "Error",
                    Message = string.IsNullOrEmpty(code) ? message : _messageDictionary.ContainsKey(code) ? _messageDictionary[code] : message
                }
            });
        }
    }
}
