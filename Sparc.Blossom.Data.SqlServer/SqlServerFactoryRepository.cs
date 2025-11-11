using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Data;
using System.Linq.Expressions;

namespace Sparc.Blossom.Data;

public class SqlServerRepository<TContext, T>(IDbContextFactory<TContext> factory) : ContextFactoryRepositoryBaseOfT<T, TContext>(factory), IRepository<T>
    where TContext : DbContext
    where T : class
{
    public IQueryable<T> Query => Factory.CreateDbContext().Set<T>().AsNoTracking();

    public IDbContextFactory<TContext> Factory { get; } = factory;

    public async Task<T?> FindAsync(object id)
    {
        using var context = Factory.CreateDbContext();
        return await context.Set<T>().FindAsync(id);
    }

    public async Task<T?> FindAsync(Expression<Func<T, bool>> expression)
    {
        using var context = Factory.CreateDbContext();
        return await context.Set<T>().Where(expression).FirstOrDefaultAsync();
    }

    public async Task<T?> FindAsync(ISpecification<T> spec)
    {
        using var context = Factory.CreateDbContext();
        return await ApplySpecification(spec, context).FirstOrDefaultAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> expression)
    {
        using var context = Factory.CreateDbContext();
        return await context.Set<T>().Where(expression).CountAsync();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        return await CountAsync(spec, default);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec)
    {
        return await AnyAsync(spec, default);
    }

    public async Task<List<T>> GetAllAsync()
    {
        using var context = Factory.CreateDbContext();
        return await context.Set<T>().ToListAsync();
    }

    public async Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        return await ListAsync(spec);
    }

    public async Task AddAsync(T item)
    {
        using var context = Factory.CreateDbContext();
        context.Set<T>().Add(item);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T item)
    {
        using var context = Factory.CreateDbContext();
        context.Set<T>().Update(item);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T item)
    {
        using var context = Factory.CreateDbContext();
        context.Set<T>().Remove(item);
        await context.SaveChangesAsync();
    }

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        using var context = Factory.CreateDbContext();
        var entity = await context.Set<T>().FindAsync(id);
        if (entity == null)
            throw new Exception($"Item with id {id} not found");

        action(entity);
        await context.SaveChangesAsync();
    }

    public async Task ExecuteAsync(T entity, Action<T> action)
    {
        using var context = Factory.CreateDbContext();
        context.Attach(entity);
        action(entity);
        await context.SaveChangesAsync();
    }

    public IQueryable<T> Include(params string[] path)
    {
        return Include(Factory.CreateDbContext().Set<T>(), path);
    }

    private IQueryable<T> Include(IQueryable<T> source, params string[] path)
    {
        foreach (var item in path)
        {
            source = source.Include(item);
        }

        return source;
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        return Factory.CreateDbContext().Set<T>().FromSqlRaw(sql, parameters);
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

        var result = Factory.CreateDbContext().Database.GetDbConnection().Query<U>(sql, p, commandType: commandType).ToList();

        return Task.FromResult(result);
    }

    public async Task AddAsync(IEnumerable<T> items)
    {
        using var context = Factory.CreateDbContext();
        foreach (var item in items)
            context.Set<T>().Add(item);

        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(IEnumerable<T> items)
    {
        using var context = Factory.CreateDbContext();
        foreach (var item in items)
            context.Set<T>().Update(item);

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        using var context = Factory.CreateDbContext();
        foreach (var item in items)
            context.Set<T>().Remove(item);

        await context.SaveChangesAsync();
    }
}
