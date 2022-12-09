using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Sparc.Blossom.Data;

public class AzureBlobRepository : IFileRepository<File>
{
    public AzureBlobRepository(BlobServiceClient client)
    {
        Client = client;
    }

    public BlobServiceClient Client { get; }

    public async Task AddAsync(File item)
    {
        var container = await GetContainer(item);
        var blob = container.GetBlobClient(item.FileName);
        if (!await blob.ExistsAsync())
            await blob.UploadAsync(item.Stream);

        item.Url = blob.Uri.AbsoluteUri;
    }

    public async Task AddAsync(IEnumerable<File> items)
    {
        await Parallel.ForEachAsync(items, async (item, token) => await AddAsync(item));
    }

    public async Task DeleteAsync(File item)
    {
        var container = await GetContainer(item);
        var blob = container.GetBlobClient(item.FileName);
        await blob.DeleteIfExistsAsync();
    }

    public async Task DeleteAsync(IEnumerable<File> items)
    {
        await Parallel.ForEachAsync(items, async (item, token) => await DeleteAsync(item));
    }

    public async Task<File?> FindAsync(object id)
    {
        if (id is not string sid || sid == null)
            throw new Exception("ID must be a folder/filename");

        var file = new File(sid);
        if (string.IsNullOrWhiteSpace(file.FolderName))
            throw new Exception("Couldn't find a folder name in the filename passed");

        var container = GetContainer(file.FolderName);
        var blob = container.GetBlobClient(file.FileName);

        if (await blob.ExistsAsync())
        {
            file.AccessType = (await container.GetAccessPolicyAsync()).Value.BlobPublicAccess.ToAccessType();
            file.Stream = new MemoryStream();
            await blob.DownloadToAsync(file.Stream);
            file.Url = blob.Uri.AbsoluteUri;
            return file;
        }

        return null;
    }
    public async Task UpdateAsync(File item)
    {
        var container = await GetContainer(item);
        var blob = container.GetBlobClient(item.FileName);
        await blob.UploadAsync(item.Stream);
        item.Url = blob.Uri.AbsoluteUri;
    }

    public async Task UpdateAsync(IEnumerable<File> items)
    {
        await Parallel.ForEachAsync(items, async (item, token) => await UpdateAsync(item));
    }

    private async Task<BlobContainerClient> GetContainer(File item)
    {
        var container = Client.GetBlobContainerClient(item.FolderName);
        await container.CreateIfNotExistsAsync(item.AccessType?.ToBlobAccessType() ?? PublicAccessType.Blob);
        return container;
    }

    private BlobContainerClient GetContainer(string containerName)
    {
        return Client.GetBlobContainerClient(containerName);
    }

}
