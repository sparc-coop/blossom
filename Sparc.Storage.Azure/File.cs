using System.IO;

namespace Sparc.Storage.Azure
{
    public class File
    {
        public File(string folderName, string fileName, AccessTypes? accessType = null, Stream? stream = null)
        {
            FolderName = folderName;
            FileName = fileName;
            AccessType = accessType ?? AccessTypes.Private;
            Stream = stream;
        }

        public File(string fileName, AccessTypes? accessType = null, Stream? stream = null)
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
}
