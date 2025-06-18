using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Sparc.Blossom.Data;

public class PouchData(CosmosDbSimpleRepository<PouchDatum> data) : IBlossomEndpoints
{
    public record GetDatasetMetadataResponse(string db_name, int doc_count, int instance_start_time, string update_seq);
    public async Task<GetDatasetMetadataResponse>  GetDbAsync(string db)
    {
        var count = data.Query(db).Count();

        var lastUpdateSequence = await data.Query
            .Where(x => x.Seq != null)
            .OrderByDescending(x => x.Seq)
            .Select(x => x.Seq)
            .CosmosFirstOrDefaultAsync();

        // Return the response
        return new(db, count, 0, lastUpdateSequence ?? "0");
    }

    public async Task<IResult> FindAsync(string db, string docid)
    {
        var doc = await data.Query(db).Where(x => x.PouchId == docid).CosmosFirstOrDefaultAsync();
        if (doc == null)
            return Results.NotFound(new { error = "not_found", reason = "missing" });

        return Results.Ok(doc.Data);
    }

    public async Task<IResult> UpsertAsync(string db, string docid, [FromBody] Dictionary<string, object?> body)
    {
        var doc = await data.Query(db).Where(x => x.PouchId == docid).CosmosFirstOrDefaultAsync();
        if (doc == null)
           doc = new PouchDatum(db, body);
        else
            doc.Data = body;

        doc.SetId(docid);
        await data.UpdateAsync(doc);
        return Results.Ok(new { ok = true, id = docid, rev = doc.Rev });
    }

    public async Task<IResult> DeleteAsync(string db, string docid, string rev)
    {
        var doc = await data.Query(db).Where(x => x.PouchId == docid && x.Rev == rev).CosmosFirstOrDefaultAsync();
        if (doc == null)
            return Results.NotFound();

        doc.Delete();
        await data.UpdateAsync(doc);
        return Results.Ok(new { ok = true, id = docid, rev = doc.Rev });
    }

    public record GetChangesRequest(List<string> doc_ids, string since, int? limit);
    public record GetChangesResult(List<GetChangesRev> rev, string id, string seq);
    public record GetChangesRev(string rev);
    public record GetChangesResponse(string last_seq, List<GetChangesResult> results);  
    public async Task<IResult> GetAllAsync(string db)
    {
        var docs = await data.Query(db).Where(x => !x.Deleted).ToListAsync();
        var rows = docs.Select(d => new
        {
            id = d.Id,
            key = d.Id,
            value = new { rev = d.Rev }
        });

        return Results.Ok(new { total_rows = rows.Count(), rows });
    }

    public record ServerMetadataVendor(string name, string version);
    public record GetServerMetadataResponse(string couchdb, string uuid, ServerMetadataVendor vendor, string version);
    public GetServerMetadataResponse GetInfo(HttpContext context)
    {
        var response = new GetServerMetadataResponse(
            "Welcome to the Cosmos API",
            "85fb71bf700c17267fef77535820e371",
            new ServerMetadataVendor("Sparc Cooperative", "2.0.1"),
            "2.0.1"
        );

        return response;
    }
    
    public async Task<GetChangesResponse> GetChangesAsync(string db, [FromQuery] string? since, [FromQuery] int? limit)
    {
        var request = new GetChangesRequest([], since ?? "0", limit);
        return await PostChangesAsync(db, request);
    }

    public async Task<GetChangesResponse> PostChangesAsync(string db, [FromBody] GetChangesRequest request)
    {
        // Build the SQL query
        var query = data.Query(db).Where(x => x.Seq != null);

        if (!string.IsNullOrEmpty(request.since) && request.since != "0")
            query = query.Where(x => string.Compare(x.Seq, request.since) > 0);

        query = query.OrderBy(x => x.Seq);

        if (request.limit.HasValue)
            query = query.Take(request.limit.Value);

        var results = await query.ToCosmosAsync();
        var last_seq = (results.LastOrDefault()?.Seq ?? request.since) ?? "0";

        var output = results
            .Select(x => new GetChangesResult([new(x.Rev)], x.Id, x.Seq!))
            .ToList();

        // Return the response
        return new GetChangesResponse(last_seq, output);
    }

    public record MissingItems(List<string> Missing);
    public async Task<Dictionary<string, MissingItems>> GetMissingItemsAsync(string db, [FromBody] Dictionary<string, List<string>> revisions)
    {
        var result = new Dictionary<string, MissingItems>();

        foreach (var id in revisions.Keys)
        {
            var revisionsList = string.Join(", ", revisions[id].Select(x => $"'{x}'"));
            var sql = $"SELECT VALUE r._rev FROM r WHERE r._rev IN ({revisionsList})";

            var existingRevisions = await data.FromSqlAsync<string>(sql, db);
            var missingRevisions = revisions[id].Except(existingRevisions).ToList();

            if (missingRevisions.Count != 0)
                result.Add(id, new MissingItems(missingRevisions));
        }

        return result;
    }
    public record BulkDocsPayload(List<Dictionary<string, object?>> Docs);
    public async Task<IResult> BulkDocs(string db, [FromBody] BulkDocsPayload payload)
    {
        var newData = payload.Docs.Select(x => new PouchDatum(db, x)).ToList();
        await data.UpdateRangeAsync(newData);

        return Results.Ok(newData.Select(d => new { ok = true, id = d.PouchId, rev = d.Rev }));
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/data";
        var group = endpoints.MapGroup(baseUrl);

        group.MapGet("/", GetInfo);
        group.MapGet("/{db}", GetDbAsync);
        group.MapGet("/{db}/_changes", GetChangesAsync);

        group.MapPost("/{db}/_changes", PostChangesAsync);
        group.MapPost("{db}/_revs_diff", GetMissingItemsAsync);
        group.MapPost("/{db}/_bulk_docs", BulkDocs);

        group.MapGet("/{db}/{docid}", FindAsync);
        group.MapPut("/{db}/{docid}", UpsertAsync);
        group.MapDelete("/{db}/{docid}", DeleteAsync);
        
        group.MapGet("/{db}/_all_docs", GetAllAsync);
    }

    public async Task UpsertDynamicAsync(string db, dynamic item)
    {
        var pk = new PartitionKeyBuilder().Add(db).Add(item._id).Build();
        await data.Client.Container.UpsertItemAsync(item, pk);
    }

    public async Task UpsertDynamicAsync(string db, IEnumerable<dynamic> items)
    {
        foreach (var item in items)
            await UpsertDynamicAsync(db, item);
    }
}
