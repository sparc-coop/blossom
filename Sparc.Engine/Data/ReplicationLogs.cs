using Microsoft.AspNetCore.Mvc;
using Sparc.Blossom.Data;
using Sparc.Blossom.Data.Pouch;

namespace Sparc.Engine;

public class ReplicationLogs(CosmosDbSimpleRepository<ReplicationLog> logs) : IBlossomEndpoints
{
    public async Task<IResult> GetAsync(string db, string id)
    {
        var log = await logs.Query(db).Where(x => x.PouchId == id).CosmosFirstOrDefaultAsync();
        if (log == null)
        {
            var dictionary = new Dictionary<string, string>
            {
                { "error", "not_found" },
                { "reason", "missing" }
            };
            return Results.NotFound(dictionary);
        }
        return Results.Ok(log);
    }

    public async Task<IResult> PutAsync(string db, string id, [FromBody] ReplicationLog log)
    {
        log.SetId(id);
        log.Db = db;
        await logs.UpdateAsync(log);
        return Results.Ok(log);
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/data");
        group.MapGet("{db}/_local/{id}", GetAsync);
        group.MapPut("{db}/_local/{id}", PutAsync);

    }
}
