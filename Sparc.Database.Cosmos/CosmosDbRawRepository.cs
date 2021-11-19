using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Sparc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sparc.Database.Cosmos
{
    public class CosmosDbRawRepository<T> : IRepository<T, string> where T : CosmosDbRoot
    {
        public IQueryable<T> Query { get; set; }
        Container Container { get; }
        string Discriminator { get; set; }
        public bool AllowSynchronousQueries { get; }
        private PartitionKey PartitionKey { get; set; }

        public CosmosDbRawRepository(CosmosDbContext context)
        {
            Container = context.Container<T>();
            Discriminator = typeof(T).Name;
            AllowSynchronousQueries = context.AllowSynchronousQueries;

            Query = Container.GetItemLinqQueryable<T>(AllowSynchronousQueries)
                .Where(x => x.Discriminator == Discriminator);
        }

        public async Task<T> FindAsync(string id)
        {
            return await Container.ReadItemAsync<T>(id, new PartitionKey(id));
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> expression)
        {
            var results = await Query.Where(expression).Take(1).ToListAsync();
            return results.FirstOrDefault();
        }

        public async Task<List<T>> GetAllAsync()
        {
            var results = await Query.ToListAsync();
            return results.ToList();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> expression)
        {
            return await Query.Where(expression).CountAsync();
        }

        // Commands
        public async Task AddAsync(T item)
        {
            await Container.CreateItemAsync(item, PartitionKey);
        }

        public async Task UpdateAsync(T item)
        {
            if (PartitionKey == default)
                await Container.UpsertItemAsync(item, new PartitionKey(item.PartitionKey));
            else
                await Container.UpsertItemAsync(item, PartitionKey);
        }

        public async Task ExecuteAsync(string id, Action<T> action)
        {
            var entity = await FindAsync(id);
            await ExecuteAsync(entity, action);
        }

        public async Task ExecuteAsync(T entity, Action<T> action)
        {
            action(entity);
            await UpdateAsync(entity);
        }

        public async Task DeleteAsync(T item)
        {
            if (PartitionKey == default)
                await Container.DeleteItemAsync<T>(item.Id, new PartitionKey(item.PartitionKey));
            else
                await Container.DeleteItemAsync<T>(item.Id, PartitionKey);
        }

        public IRepository<T, string> Partition(string partitionKey)
        {
            PartitionKey = new PartitionKey(partitionKey);
            var options = new QueryRequestOptions { PartitionKey = PartitionKey };
            Query = Container.GetItemLinqQueryable<T>(AllowSynchronousQueries, requestOptions: options)
                .Where(x => x.Discriminator == Discriminator);
            return this;
        }

        public async Task ExecuteAsync(object id, Action<T> action)
        {
            var entity = await FindAsync(id);
            if (entity != null)
                await ExecuteAsync(entity, action);
        }

        public async Task<T?> FindAsync(object id)
        {
            if (id is string str)
                return await FindAsync(str);
            
            return null;
        }

        public async Task<IEnumerable<T>> FromSql(string databaseName, string sql)
        {
            return await FromSql<T>(databaseName, sql);
        }

        public async Task<IEnumerable<U>> FromSql<U>(string databaseName, string sql)
        {
            var results = Container.GetItemQueryIterator<U>(new QueryDefinition(sql));
            var list = await results.ReadNextAsync();

            return list;
        }
    }
}
