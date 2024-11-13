namespace Sparc.Blossom.Data;

public class BlossomFile
{
    public BlossomFile(string folderName, string fileName, AccessTypes? accessType = null, Stream? stream = null)
    {
        FolderName = folderName;
        FileName = fileName;
        AccessType = accessType ?? AccessTypes.Private;
        Stream = stream;
    }

    public BlossomFile(string fileName, AccessTypes? accessType = null, Stream? stream = null)
    {
        FolderName = Path.GetDirectoryName(fileName) ?? string.Empty;
        FileName = Path.GetFileName(fileName);
        AccessType = accessType ?? AccessTypes.Private;
        Stream = stream;
    }

    public string FolderName { get; set; }
    public string FileName { get; set; }
    public AccessTypes? AccessType { get; set; }
    public Stream? Stream { get; set; }
    public string? ContentType { get; set; }
    public string? Url { get; set; }
}
