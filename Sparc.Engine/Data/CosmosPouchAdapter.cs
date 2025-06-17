using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Data.Pouch;
using System.Data;
using System.Text.Json;

namespace Sparc.Blossom.Data;

public class CosmosPouchAdapter(CosmosDbDynamicRepository<Datum> data, CosmosDbSimpleRepository<ReplicationLog> checkpoints) : IBlossomCloudApi
{
    public CosmosDbDynamicRepository<Datum> Data { get; } = data;
    public CosmosDbSimpleRepository<ReplicationLog> Checkpoints { get; } = checkpoints;
    public record GetDatasetMetadataResponse(string db_name, int doc_count, int instance_start_time, string update_seq);
    public async Task<GetDatasetMetadataResponse>  GetDbAsync(string db)
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

    

    public record GetChangesRequest(List<string> doc_ids, string since, int? limit);
    public record GetChangesResult(List<GetChangesRev> rev, string id, string seq);
    public record GetChangesRev(string rev);
    public record GetChangesResponse(string last_seq, List<GetChangesResult> results);  
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
    public async Task<IResult> GetCheckpointAsync(string db, string id)
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
    public async Task<IResult> PutCheckpointAsync(string db, string id, [FromBody] ReplicationLog log)
    {
        log.PouchId = id;
        log.Id = id;
        log.TenantId = "sparc";
        log.UserId = "sparc-admin";
        log.DatabaseId = db;
        await Checkpoints.UpsertAsync(log, db);
        return Results.Ok(log);
    }
    public async Task<GetChangesResponse> GetChangesAsync(string db, [FromQuery] string? since, [FromQuery] int? limit)
    {
        var request = new GetChangesRequest(new List<string>(), since ?? "0", limit);
        return await PostChangesAsync(db, request);
    }
    public async Task<GetChangesResponse> PostChangesAsync(string db, [FromBody] GetChangesRequest request)
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
    public record BulkDocsPayload(List<Dictionary<string, JsonElement>> Docs);
    public async Task<IResult> BulkDocs(string db, [FromBody] BulkDocsPayload payload)
    {
        foreach (var doc in payload.Docs)
        {
            var objectDictionary = ConvertDocToDictionary(doc);
            objectDictionary["id"] = objectDictionary["_id"];
            
            EnsurePartitionKey(objectDictionary, db);

            dynamic dynamicDoc = new System.Dynamic.ExpandoObject();
            var dynamicDict = (IDictionary<string, object?>)dynamicDoc;
            foreach (var kvp in objectDictionary)
            {
                dynamicDict[kvp.Key] = kvp.Value;
            }

            await Data.UpsertAsync(dynamicDoc, db);
        }

        return Results.Ok(payload.Docs.Select(d => new { ok = true, id = d["_id"], rev = d["_rev"] }));
    }

    private void EnsurePartitionKey(Dictionary<string, object?> dic, string db)
    {
        if (!dic.ContainsKey("_tenantId"))
        {
            dic["_tenantId"] = "sparc";
        }

        if (!dic.ContainsKey("_userId"))
        {
            dic["_userId"] = "sparc-admin";
        }

        if (!dic.ContainsKey("_databaseId"))
        {
            dic["_databaseId"] = db;
        }
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/data";
        var group = endpoints.MapGroup(baseUrl);

        group.MapGet("/", GetInfo);
        group.MapGet("{db}", GetDbAsync);
        group.MapGet("{db}/_local/{id}", GetCheckpointAsync);
        group.MapPut("{db}/_local/{id}", PutCheckpointAsync);
        group.MapPost("/{db}/_changes", PostChangesAsync);
        group.MapGet("/{db}/_changes", GetChangesAsync);
        group.MapPost("{db}/_revs_diff", GetRevsDiff);

        group.MapPost("/{db}/_bulk_docs", BulkDocs);
        //group.MapPut("/{db}", CreateDatabase);
        //group.MapPost("/{db}", CreateDocument);
        group.MapPut("/{db}/{docid}", CreateOrUpdateDocument);
        group.MapGet("/{db}/{docid}", GetDocument);
        group.MapDelete("/{db}/{docid}", DeleteDocument);
        
        group.MapGet("/{db}/_all_docs", GetAllDocs);
       
        
        
    }

    

    private static string IncrementRev(string rev)
    {
        var parts = rev.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var number))
            return $"1-{Guid.NewGuid():N}";

        return $"{number + 1}-{Guid.NewGuid():N}";
    }
    private static Dictionary<string, object?> ConvertDocToDictionary(Dictionary<string, JsonElement> doc)
    {
        var result = new Dictionary<string, object?>();
        foreach (var kvp in doc)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    result[key] = value.GetString();
                    break;
                case JsonValueKind.Number:
                    if (value.TryGetInt64(out var l))
                        result[key] = l;
                    else if (value.TryGetDouble(out var d))
                        result[key] = d;
                    else
                        result[key] = value.GetRawText();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    result[key] = value.GetBoolean();
                    break;
                case JsonValueKind.Object:
                    result[key] = ConvertDocToDictionary(System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText())!);
                    break;
                case JsonValueKind.Array:
                    var arr = new List<object?>();
                    foreach (var item in value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                            arr.Add(ConvertDocToDictionary(System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText())!));
                        else if (item.ValueKind == JsonValueKind.Array)
                            arr.Add(System.Text.Json.JsonSerializer.Deserialize<List<object?>>(item.GetRawText()));
                        else if (item.ValueKind == JsonValueKind.String)
                            arr.Add(item.GetString());
                        else if (item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var arrL))
                            arr.Add(arrL);
                        else if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var arrD))
                            arr.Add(arrD);
                        else if (item.ValueKind == JsonValueKind.True || item.ValueKind == JsonValueKind.False)
                            arr.Add(item.GetBoolean());
                        else
                            arr.Add(item.GetRawText());
                    }
                    result[key] = arr;
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    result[key] = null;
                    break;
                default:
                    result[key] = value.GetRawText();
                    break;
            }
        }
        return result;
    }
}
