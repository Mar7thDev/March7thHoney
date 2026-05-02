using CastoriceHoney.Server.Enums.Language;

namespace CastoriceHoney.Server.Internationalization;

[AttributeUsage(AttributeTargets.Class)]
public class PluginLanguageAttribute(ProgramLanguageTypeEnum languageType) : Attribute
{
    public ProgramLanguageTypeEnum LanguageType { get; } = languageType;
}