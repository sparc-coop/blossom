using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Sparc.Blossom.Data;
using System.Data;

namespace Sparc.Blossom.Cloud.Data;

public class CosmosPouchAdapter(CosmosDbSimpleRepository<Datum> data) : IBlossomApi
{
    public CosmosDbSimpleRepository<Datum> Data { get; } = data;

    public record GetDatasetMetadataResponse(string db_name, int doc_count, int instance_start_time, string update_seq);
    public async Task<GetDatasetMetadataResponse> GetDb(string db)
    {
        var count = Data.Query(db).Count();

        var lastUpdateSequence = await Data.Query
            .Where(x => x.Seq != null)
            .OrderByDescending(x => x.Seq)
            .Select(x => x.Seq)
            .CosmosFirstOrDefaultAsync();

        // Return the response
        return new(db, count, 0, lastUpdateSequence ?? "0");
    }

    public record MissingItems(List<string> Missing);
    public async Task<Dictionary<string, MissingItems>> GetRevsDiff(string db, [FromBody] Dictionary<string, List<string>> revisions)
    {
        var result = new Dictionary<string, MissingItems>();

        foreach (var id in revisions.Keys)
        {
            var revisionsList = string.Join(", ", revisions[id].Select(x => $"'{x}'"));
            var sql = $"SELECT VALUE r._rev FROM r WHERE r._rev IN ({revisionsList})";

            var existingRevisions = await Data.FromSqlAsync<string>(sql, db);
            var missingRevisions = revisions[id].Except(existingRevisions).ToList();

            if (missingRevisions.Count != 0)
                result.Add(id, new MissingItems(missingRevisions));
        }

        return result;
    }

    public record GetChangesRequest(List<string> doc_ids, string since, int? limit);
    public record GetChangesResult(List<GetChangesRev> rev, string id, string seq);
    public record GetChangesRev(string rev);
    public record GetChangesResponse(string last_seq, List<GetChangesResult> results);

    public async Task<GetChangesResponse> GetChanges(string db, [FromBody] GetChangesRequest request)
    {
        // Build the SQL query
        var query = Data.Query.Where(x => x.Seq != null);
        if (!string.IsNullOrEmpty(request.since) && request.since != "0")
            query = query.Where(x => string.Compare(x.Seq, request.since) > 0);

        if (request.limit.HasValue)
            query = query.Take(request.limit.Value);

        query = query.OrderBy(x => x.Seq);

        var results = await query.ToCosmosAsync();
        var last_seq = (results.LastOrDefault()?.Seq ?? request.since) ?? "0";

        var output = results
            .Select(x => new GetChangesResult([new(x.Rev)], x.Id, x.Seq))
            .ToList();

        // Return the response
        return new GetChangesResponse(last_seq, output);
    }
    //public async Task<Results<ReplicationLog>> GetCheckpoint(string db, string id)
    //{
    //    // Attempt to read the item from Cosmos DB
    //    var item = await Data.Query(db).Where(x => x.Id == id).CosmosFirstOrDefaultAsync();
    //    if (item == null)
    //    {
    //        var dictionary = new Dictionary<string, string>
    //    {
    //        { "error", "not_found" },
    //        { "reason", "missing" }
    //    };

    //        return Results.NotFound(dictionary);
    //    }
    //    return Results.Ok(item);
    //}

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/data";
        var group = endpoints.MapGroup(baseUrl);

        group.MapGet("{db}", GetDb);
        group.MapPost("{db}/_revs_diff", GetRevsDiff);
        group.MapPost("{db}/_changes", GetChanges);
        //group.MapGet("{db}/_local/{id}", GetCheckpoint);
    }
}
