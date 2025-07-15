using Ardalis.Specification;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Sparc.Blossom.Data.AzureStorage;

namespace Sparc.Blossom.Data;

public class AzureBlobRepository(BlobServiceClient client) : IRepository<BlossomFile>
{
    public BlobServiceClient Client { get; } = client;

    public IQueryable<BlossomFile> Query => throw new NotImplementedException();

    public async Task AddAsync(BlossomFile item)
    {
        var container = await GetContainer(item);
        var blob = container.GetBlobClient(item.FileName);
        if (!await blob.ExistsAsync() && item.Stream != null)
        {
            item.Stream.Position = 0;
            await blob.UploadAsync(item.Stream);
        }
        item.Url = blob.Uri.AbsoluteUri;
    }

    public async Task AddAsync(BlossomFile item, IProgress<int> progress)
    {
        if (item.Stream == null)
            throw new Exception("Stream is not attached to the BlossomFile");

        var container = await GetContainer(item);
        var blob = container.GetBlobClient(item.FileName);

        var blockSize = 81920; // 80 KB
        var totalBytes = item.Stream.Length;
        var uploadedBytes = 0L;
        var buffer = new byte[blockSize];

        using var uploadStream = new MemoryStream();
        int read;
        while ((read = await item.Stream.ReadAsync(buffer)) > 0)
        {
            await uploadStream.WriteAsync(buffer.AsMemory(0, read));
            uploadedBytes += read;

            var percent = (int)(uploadedBytes * 100 / totalBytes);
            progress.Report(percent);
        }

        uploadStream.Position = 0;

        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = item.ContentType
        };

        await blob.UploadAsync(uploadStream, blobHttpHeaders);

        item.Url = blob.Uri.AbsoluteUri;
    }

    public async Task AddAsync(IEnumerable<BlossomFile> items)
    {
        await Parallel.ForEachAsync(items, async (item, token) => await AddAsync(item));
    }

    public Task<bool> AnyAsync(ISpecification<BlossomFile> spec)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync(ISpecification<BlossomFile> spec)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(BlossomFile item)
    {
        var container = await GetContainer(item);
        var blob = container.GetBlobClient(item.FileName);
        await blob.DeleteIfExistsAsync();
    }

    public async Task DeleteAsync(IEnumerable<BlossomFile> items)
    {
        await Parallel.ForEachAsync(items, async (item, token) => await DeleteAsync(item));
    }

    public Task ExecuteAsync(object id, Action<BlossomFile> action)
    {
        throw new NotImplementedException();
    }

    public Task ExecuteAsync(BlossomFile entity, Action<BlossomFile> action)
    {
        throw new NotImplementedException();
    }

    public async Task<BlossomFile?> FindAsync(object id)
    {
        if (id is not string sid || sid == null)
            throw new Exception("ID must be a folder/filename");

        var file = new BlossomFile(sid);
        if (string.IsNullOrWhiteSpace(file.FolderName))
            throw new Exception("Couldn't find a folder name in the filename passed");

        var container = GetContainer(file.FolderName);
        var blob = container.GetBlobClient(file.FileName);

        if (await blob.ExistsAsync())
        {
            file.AccessType = (await container.GetAccessPolicyAsync()).Value.BlobPublicAccess.ToAccessType();
            file.Stream = new MemoryStream();
            await blob.DownloadToAsync(file.Stream);
            file.Stream.Position = 0; // Reset stream position after download
            file.Url = blob.Uri.AbsoluteUri;
            file.LastModified = blob.GetProperties().Value.LastModified.UtcDateTime;
            return file;
        }

        return null;
    }

    public Task<BlossomFile?> FindAsync(ISpecification<BlossomFile> spec)
    {
        throw new NotImplementedException();
    }

    public IQueryable<BlossomFile> FromSqlRaw(string sql, params object[] parameters)
    {
        throw new NotImplementedException();
    }

    public Task<List<BlossomFile>> GetAllAsync(ISpecification<BlossomFile> spec)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(BlossomFile item)
    {
        var container = await GetContainer(item);
        var blob = container.GetBlobClient(item.FileName);
        await blob.UploadAsync(item.Stream);
        item.Url = blob.Uri.AbsoluteUri;
    }

    public async Task UpdateAsync(IEnumerable<BlossomFile> items)
    {
        await Parallel.ForEachAsync(items, async (item, token) => await UpdateAsync(item));
    }

    private async Task<BlobContainerClient> GetContainer(BlossomFile item)
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
