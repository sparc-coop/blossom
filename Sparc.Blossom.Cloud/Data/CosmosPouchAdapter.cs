using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Data.Pouch;
using System.Data;

namespace Sparc.Blossom.Data;

public class CosmosPouchAdapter(CosmosDbSimpleRepository<Datum> data, CosmosDbSimpleRepository<ReplicationLog> checkpoints) : IBlossomCloudApi
{
    public CosmosDbSimpleRepository<Datum> Data { get; } = data;
    public CosmosDbSimpleRepository<ReplicationLog> Checkpoints { get; } = checkpoints;
    public record GetDatasetMetadataResponse(string db_name, int doc_count, int instance_start_time, string update_seq);
    public async Task<GetDatasetMetadataResponse>  GetDb(string db)
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

    public async Task<IResult> GetDocument(string db, string docid)
    {
        var doc = await Data.Query(db).Where(x => x.Id == docid).CosmosFirstOrDefaultAsync();
        if (doc == null)
            return Results.NotFound(new { error = "not_found", reason = "missing" });

        return Results.Ok(doc);
    }

    public async Task<IResult> CreateOrUpdateDocument(string db, string docid, [FromBody] Datum body)
    {
        body.Id = docid;
        await Data.UpsertAsync(body, db);
        return Results.Ok(new { ok = true, id = docid, rev = body.Rev });
    }

    public async Task<IResult> DeleteDocument(string db, string docid, string rev)
    {
        var doc = await Data.Query(db).Where(x => x.Id == docid).CosmosFirstOrDefaultAsync();
        if (doc == null)
            return Results.NotFound();

        doc.Deleted = true;
        doc.Rev = IncrementRev(rev);

        await Data.UpsertAsync(doc, db);
        return Results.Ok(new { ok = true, id = docid, rev = doc.Rev });
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
    public record BulkDocsPayload(List<Dictionary<string, object>> Docs);
    public async Task<IResult> BulkDocs(string db, [FromBody] BulkDocsPayload payload)
    {
        foreach (var doc in payload.Docs)
        {
            var datum = new Datum
            {
                Id = doc["_id"].ToString()!,
                Rev = doc["_rev"].ToString()!,
                Deleted = doc.ContainsKey("_deleted") && (bool)doc["_deleted"],
                TenantId = "sparc",
                UserId = "sparc-admin",
                DatabaseId = db,
                Doc = doc
            };

            await Data.UpsertAsync(datum, db);
        }

        return Results.Ok(payload.Docs.Select(d => new { ok = true, id = d["_id"], rev = d["_rev"] }));
    }
    public async Task<IResult> GetAllDocs(string db)
    {
        var docs = await Data.Query(db).Where(x => !x.Deleted).ToListAsync();
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


    public async Task<GetServerMetadataResponse> GetInfo(HttpContext context)
    {
        var response = new GetServerMetadataResponse(
            "Welcome to the Cosmos API",
            "85fb71bf700c17267fef77535820e371",
            new ServerMetadataVendor("Sparc Cooperative", "2.0.1"),
            "2.0.1"
        );

        return response;
    }

    public async Task<IResult> GetCheckpoint(string db, string id)
    {
        var log = await Checkpoints.Query(db).Where(x => x.PouchId == id).CosmosFirstOrDefaultAsync();
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

    public async Task<IResult> PutCheckpoint(string db, string id, [FromBody] ReplicationLog log)
    {
        log.PouchId = id;
        log.Id = id;
        log.TenantId = "sparc";
        log.UserId = "sparc-admin";
        log.DatabaseId = db;
        await Checkpoints.UpsertAsync(log, db);
        return Results.Ok(log);
    }
    public async Task<GetChangesResponse> GetChanges(string db, [FromQuery] string? since, [FromQuery] int? limit)
    {
        var request = new GetChangesRequest(new List<string>(), since ?? "0", limit);
        return await PostChanges(db, request);
    }
    public async Task<GetChangesResponse> PostChanges(string db, [FromBody] GetChangesRequest request)
    {
        // Build the SQL query
        var query = Data.Query(db).Where(x => x.Seq != null);
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

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/data";
        var group = endpoints.MapGroup(baseUrl);

        group.MapGet("/", GetInfo);
        group.MapGet("{db}", GetDb);
        group.MapGet("{db}/_local/{id}", GetCheckpoint);
        group.MapPut("{db}/_local/{id}", PutCheckpoint);
        group.MapPost("/{db}/_changes", PostChanges);
        group.MapGet("/{db}/_changes", GetChanges);
        //group.MapPut("/{db}", CreateDatabase);
        //group.MapPost("/{db}", CreateDocument);
        group.MapPut("/{db}/{docid}", CreateOrUpdateDocument);
        group.MapGet("/{db}/{docid}", GetDocument);
        group.MapDelete("/{db}/{docid}", DeleteDocument);
        group.MapPost("/{db}/_bulk_docs", BulkDocs);
        group.MapGet("/{db}/_all_docs", GetAllDocs);
       
        group.MapPost("{db}/_revs_diff", GetRevsDiff);
        
    }

    

    private static string IncrementRev(string rev)
    {
        var parts = rev.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var number))
            return $"1-{Guid.NewGuid():N}";

        return $"{number + 1}-{Guid.NewGuid():N}";
    }
}
