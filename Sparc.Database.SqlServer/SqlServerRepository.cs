using Dapper;
using Microsoft.EntityFrameworkCore;
using Sparc.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sparc.Database.SqlServer
{
    public class SqlServerRepository<T> : IRepository<T> where T : class
    {
        protected readonly DbContext context;
        protected DbSet<T> Command => context.Set<T>();
        public IQueryable<T> Query => context.Set<T>().AsNoTracking();

        public SqlServerRepository(DbContext context)
        {
            this.context = context;
        }

        public async Task<T?> FindAsync(object id)
        {
            return await Command.FindAsync(id);
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> expression)
        {
            return await Command.Where(expression).FirstOrDefaultAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> expression)
        {
            return await Command.Where(expression).CountAsync();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await Command.ToListAsync();
        }

        public async Task AddAsync(T item)
        {
            Command.Add(item);
            await CommitAsync();
        }

        public async Task UpdateAsync(T item)
        {
            Command.Update(item);
            await CommitAsync();
        }

        public async Task DeleteAsync(T item)
        {
            Command.Remove(item);
            await CommitAsync();
        }

        public async Task ExecuteAsync(object id, Action<T> action)
        {
            var entity = await context.Set<T>().FindAsync(id);
            if (entity == null)
                throw new Exception($"Item with id {id} not found");

            action(entity);
        }

        public Task ExecuteAsync(T entity, Action<T> action)
        {
            action(entity);
            return Task.CompletedTask;
        }

        public IQueryable<T> Include(params string[] path)
        {
            return Include(Command, path);
        }

        private IQueryable<T> Include(IQueryable<T> source, params string[] path)
        {
            foreach (var item in path)
            {
                source = source.Include(item);
            }

            return source;
        }

        public async Task CommitAsync()
        {
            await context.SaveChangesAsync();
        }

        public async Task<List<T>> FromSqlAsync(string sql, params (string, object)[] parameters)
        {
            return await FromSqlAsync<T>(sql, parameters);
        }

        public Task<List<U>> FromSqlAsync<U>(string sql, params (string, object)[] parameters)
        {
            var isStoredProcedure = sql.StartsWith("EXEC ");
            var commandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text;

            if (isStoredProcedure)
                sql = sql.Replace("EXEC ", "");

            var p = new DynamicParameters();
            if (parameters != null)
                foreach (var parameter in parameters)
                {
                    var key = (parameter.Item1.Contains("@") ? "" : "@") + parameter.Item1;
                    p.Add(key, parameter.Item2);
                }

            var result = context.Database.GetDbConnection().Query<U>(sql, p, commandType: commandType).ToList();

            return Task.FromResult(result);
        }
    }
}
