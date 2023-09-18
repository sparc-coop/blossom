using Microsoft.AspNetCore.Http;
using Sparc.Blossom.Data;

namespace Sparc.Blossom;

internal class BlossomCommandFilter<T>(IRepository<T> repository) : IEndpointFilter
{
    private readonly IRepository<T> _repository = repository;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var id = context.HttpContext.Request.RouteValues["id"];
        if (id == null)
            return Results.BadRequest();

        var entity = await _repository.FindAsync(id);
        if (entity == null)
            return Results.NotFound();

        //context.Arguments.Remove(id);
        //context.Arguments.Add(entity);
        context.HttpContext.Items.Add("entity", entity);

        var result = await next(context);

        await _repository.UpdateAsync(entity);

        return result;
    }
}