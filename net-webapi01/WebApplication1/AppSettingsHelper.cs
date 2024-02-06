public static class AppSettingsHelper
{
    private static IConfiguration _config;
    public static void ConfigureSetting(IConfiguration config)
    {
        _config = config;
    }

    private static string Setting(string Key)
    {
        return AESHelpers.Decrypt(_config.GetSection(Key).Value!);
    }

    static Type GetSettingAsType<Type>(object obj, Func<object, Type> callerConverter)
    {

        if (obj != null)
        {
            Type value = default!;
            try
            {
                value = callerConverter(obj);
            }
            catch
            {
                value = default!;
            }
            return value;
        }
        else
            return default!;
    }

    public static int GetSettingAsInteger(string key)
    {
        return GetSettingAsType<int>(Setting(key), obj => Convert.ToInt32(obj));
    }
    public static string GetSettingAsString(string key)
    {
        return GetSettingAsType<string>(Setting(key), obj => Convert.ToString(obj)!);
    }
    public static bool? GetSettingAsBool(string key)
    {
        return GetSettingAsType<bool?>(Setting(key), obj => Convert.ToBoolean(obj));
    }

}