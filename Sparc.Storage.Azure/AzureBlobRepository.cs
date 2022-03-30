using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Sparc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sparc.Storage.Azure
{
    public class AzureBlobRepository : IRepository<File>
    {
        public AzureBlobRepository(BlobServiceClient client)
        {
            Client = client;
        }

        public IQueryable<File> Query => throw new NotImplementedException();

        public BlobServiceClient Client { get; }

        public async Task AddAsync(File item)
        {
            var container = await GetContainer(item);
            var blob = container.GetBlobClient(item.FileName);
            if (!await blob.ExistsAsync())
                await blob.UploadAsync(item.Stream);

            item.Url = blob.Uri.AbsoluteUri;
        }

        public void BeginBulkOperation()
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(File item)
        {
            var container = await GetContainer(item);
            var blob = container.GetBlobClient(item.FileName);
            await blob.DeleteIfExistsAsync();
        }

        public Task ExecuteAsync(object id, Action<File> action)
        {
            throw new NotImplementedException();
        }

        public Task ExecuteAsync(File entity, Action<File> action)
        {
            throw new NotImplementedException();
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

        public Task<List<File>> FromSqlAsync(string sql, params (string, object)[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<List<U>> FromSqlAsync<U>(string sql, params (string, object)[] parameters)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(File item)
        {
            var container = await GetContainer(item);
            var blob = container.GetBlobClient(item.FileName);
            await blob.UploadAsync(item.Stream);
            item.Url = blob.Uri.AbsoluteUri;
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
}
