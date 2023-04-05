using Microsoft.AspNetCore.Http;
using Sparc.Blossom.Data;

namespace Sparc.Blossom;

internal class BlossomCommandFilter<T> : IEndpointFilter
{
    private readonly IRepository<T> _repository;

    public BlossomCommandFilter(IRepository<T> repository)
    {
        _repository = repository;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var id = context.GetArgument<string>(0);
        var entity = await _repository.FindAsync(id);
        if (entity == null)
            return Results.NotFound();

        context.Arguments.Remove(id);
        context.Arguments.Insert(0, entity);
        context.HttpContext.Items.Add("entity", entity);

        var result = await next(context);

        await _repository.UpdateAsync(entity);

        return result;
    }
}