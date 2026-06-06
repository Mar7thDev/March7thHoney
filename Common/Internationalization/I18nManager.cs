using March7thHoney.Internationalization.Message;
using March7thHoney.Util;

namespace March7thHoney.Internationalization;

public static class I18NManager
{
    public static Logger Logger = new("I18nManager");

    public static object Language { get; set; } = new LanguageEN();

    public static void LoadLanguage()
    {
        var languageStr = "March7thHoney.Internationalization.Message.Language" +
                          ConfigManager.Config.ServerOption.Language;
        var languageType = Type.GetType(languageStr);
        if (languageType == null)
        {
            Logger.Warn("Language not found, fallback to EN");
            // fallback to English
            languageType = Type.GetType("March7thHoney.Internationalization.Message.LanguageEN")!;
        }

        var language = Activator.CreateInstance(languageType) ?? throw new Exception("Language not found");
        Language = language;

        Logger.Info(Translate("Server.ServerInfo.LoadedItem", Translate("Word.Language")));
    }

    public static string Translate(string key, params string[] args)
    {
        var result = GetNestedPropertyValue(Language, key) ?? key;

        var index = 0;

        return args.Aggregate(result, (current, arg) => current.Replace("{" + index++ + "}", arg));
    }

    public static string TranslateAsCertainLang(string langStr, string key, params string[] args)
    {
        var languageStr = "March7thHoney.Internationalization.Message.Language" +
                          langStr;
        var languageType = Type.GetType(languageStr) ??
                           Type.GetType("March7thHoney.Internationalization.Message.LanguageEN")!;
        var language = Activator.CreateInstance(languageType) ?? throw new Exception("Language not found");

        List<object> langs = [language];

        var result = langs.Select(lang => GetNestedPropertyValue(lang, key)).OfType<string>().FirstOrDefault() ?? key;

        var index = 0;

        return args.Aggregate(result, (current, arg) => current.Replace("{" + index++ + "}", arg));
    }


    public static string? GetNestedPropertyValue(object? obj, string propertyName)
    {
        foreach (var part in propertyName.Split('.'))
        {
            if (obj == null) return null;

            var type = obj.GetType();
            var property = type.GetProperty(part);
            if (property == null) return null;

            obj = property.GetValue(obj, null);
        }

        return (string?)obj;
    }
}