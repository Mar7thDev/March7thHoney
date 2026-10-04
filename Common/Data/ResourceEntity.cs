namespace March7thHoney.Data;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ResourceEntity : Attribute
{
    public ResourceEntity(string fileName, bool isMultifile = false)
    {
        if (isMultifile)
            FileName = new List<string>(fileName.Split(','));
        else
            FileName = [fileName];
    }

    public ResourceEntity(string fileName)
    {
        FileName = [fileName];
    }

    public List<string> FileName { get; private set; }

    /// <summary>
    /// Comma-separated companion tables absent in some client archives. Existing
    /// files must still parse; required tables in a multi-file resource stay required.
    /// </summary>
    public string OptionalFileNames { get; set; } = "";
}
