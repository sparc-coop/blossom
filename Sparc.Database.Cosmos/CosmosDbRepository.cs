using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Sparc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sparc.Plugins.Database.Cosmos
{
    public class CosmosDbRepository<T> : IRepository<T> where T : class, IRoot<string>
    {
        public IQueryable<T> Query { get; }
        private DbContext Context { get; }
        public CosmosDbDatabaseProvider DbProvider { get; }

        private static bool IsCreated;

        public CosmosDbRepository(DbContext context, CosmosDbDatabaseProvider dbProvider)
        {
            Context = context;
            DbProvider = dbProvider;
            if (!IsCreated)
            {
                Context.Database.EnsureCreatedAsync().Wait();
                IsCreated = true;
            }
            //Mediator = mediator;
            Query = context.Set<T>();
        }

        public async Task<T?> FindAsync(object id)
        {
            if (id is string sid)
                return Context.Set<T>().FirstOrDefault(x => x.Id == sid);

            return await Context.Set<T>().FindAsync(id);
        }

        public async Task AddAsync(T item)
        {
            Context.Add(item);
            await SaveChangesAsync();
        }

        public async Task UpdateAsync(T item)
        {
            var exists = await Query.Where(x => x.Id == item.Id).CountAsync();
            if (exists > 0)
            {
                Context.Add(item);
                Context.Update(item);
                await Context.SaveChangesAsync();
            }
            else
            {
                await AddAsync(item);
            }
        }

        public async Task ExecuteAsync(object id, Action<T> action)
        {
            var entity = await FindAsync(id);
            if (entity == null)
                throw new Exception($"Item with id {id} not found");

            await ExecuteAsync(entity, action);
        }

        public async Task ExecuteAsync(T entity, Action<T> action)
        {
            action(entity);
            await UpdateAsync(entity);
        }

        public async Task DeleteAsync(T item)
        {
            Context.Set<T>().Remove(item);
            await Context.SaveChangesAsync();
        }

        private async Task<int> SaveChangesAsync()
        {
            return await Context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<List<T>> FromSqlAsync(string sql, params (string, object)[] parameters)
        {
            return await FromSqlAsync(sql, null, parameters);
        }

        public async Task<List<U>> FromSqlAsync<U>(string sql, params (string, object)[] parameters)
        {
            return await FromSqlAsync<U>(sql, null, parameters);
        }

        public async Task<List<T>> FromSqlAsync(string sql, string? partitionKey, params (string, object)[] parameters)
        {
            var list = await FromSqlAsync<T>(sql, partitionKey, parameters);

            // Handle Cosmos conflicts w/ internal ID (with discriminator) and external ID
            foreach (var item in list)
                item.Id = item.Id.Replace($"{typeof(T).Name}|", "");

            return list;
        }

        public async Task<List<U>> FromSqlAsync<U>(string sql, string? partitionKey, params (string, object)[] parameters)
        {
            var container = DbProvider.Database.GetContainer(Context.GetType().Name);
            var requestOptions = partitionKey == null
                ? null
                : new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) };

            var query = new QueryDefinition(sql);
            if (parameters != null)
                foreach (var parameter in parameters)
                {
                    var key = (parameter.Item1.Contains("@") ? "" : "@") + parameter.Item1;
                    query = query.WithParameter(key, parameter.Item2);
                }

            var results = container.GetItemQueryIterator<U>(query,
                requestOptions: requestOptions);

            var list = new List<U>();

            while (results.HasMoreResults)
                list.AddRange(await results.ReadNextAsync());

            return list;
        }

        public IQueryable<T> PartitionQuery(string partitionKey)
        {
            return Query.WithPartitionKey(partitionKey);
        }
    }
}
