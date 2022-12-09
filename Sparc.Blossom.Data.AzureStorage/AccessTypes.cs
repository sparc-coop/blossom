using Azure.Storage.Blobs.Models;

namespace Sparc.Blossom.Data;

public enum AccessTypes
{
    Public,
    PublicAndDiscoverable,
    Private
}

public static class AccessTypesExtensions
{
    public static PublicAccessType ToBlobAccessType(this AccessTypes accessType)
    {
        switch (accessType)
        {
            case AccessTypes.Public:
                return PublicAccessType.Blob;
            case AccessTypes.PublicAndDiscoverable:
                return PublicAccessType.BlobContainer;
            case AccessTypes.Private:
                return PublicAccessType.None;
            default:
                break;
        }

        return PublicAccessType.Blob;
    }

    public static AccessTypes ToAccessType(this PublicAccessType accessType)
    {
        switch (accessType)
        {
            case PublicAccessType.Blob:
                return AccessTypes.Public;
            case PublicAccessType.BlobContainer:
                return AccessTypes.PublicAndDiscoverable;
            case PublicAccessType.None:
                return AccessTypes.Private;
            default:
                break;
        }

        return AccessTypes.Public;
    }
}
