namespace Sparc.Blossom;

public class BlossomFile
{
    public BlossomFile(string folderName, string fileName, AccessTypes? accessType = null, Stream? stream = null)
    {
        FolderName = folderName;
        FileName = fileName;
        AccessType = accessType ?? AccessTypes.Private;
        Stream = stream;
    }

    public BlossomFile(Uri sourceUri, string fileName)
    {
        fileName = fileName.Replace(sourceUri.AbsoluteUri, "");
        FolderName = fileName.Split('/').First();
        FileName = fileName.Replace(FolderName + "/", "");
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
    public DateTime? LastModified { get; set; }
}
