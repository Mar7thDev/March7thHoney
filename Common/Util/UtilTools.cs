using System.Globalization;

namespace March7thHoney.Util;

public static class UtilTools
{
    public static string GetCurrentLanguage()
    {
        var uiCulture = CultureInfo.CurrentUICulture;
        return uiCulture.Name switch
        {
            "zh-CN" => "CHS",
            "zh-TW" => "CHT",
            "ja-JP" => "JP",
            _ => "EN"
        };
    }
}