using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Sparc.Blossom.Data;
using System.Data;

namespace Sparc.Blossom.Cloud.Data;

public class CosmosPouchAdapter(CosmosDbSimpleRepository<Datum> data) : IBlossomApi
{
    public CosmosDbSimpleRepository<Datum> Data { get; } = data;

    public record GetDatasetMetadataResponse(string db_name, int doc_count, int instance_start_time, string update_seq);
    public async Task<GetDatasetMetadataResponse> GetDb(string name)
    {
        var count = Data.Query(name).Count();

        var query = "SELECT TOP 1 c._seq FROM c WHERE ISNULL(c._seq) = false ORDER BY c._seq DESC";
        var result = await Data.FromSqlAsync<dynamic>(query, name);
        var lastUpdateSequence = result.FirstOrDefault()?._seq;

        // Return the response
        return new(name, count, 0, lastUpdateSequence);
    }

    public record MissingItems(List<string> Missing);
    public async Task<Dictionary<string, MissingItems>> GetRevsDiff(string datasetId, [FromBody] Dictionary<string, List<string>> revisions)
    {
        var result = new Dictionary<string, MissingItems>();

        foreach (var id in revisions.Keys)
        {
            var revisionsList = string.Join(", ", revisions[id].Select(x => $"'{x}'"));
            var sql = $"SELECT VALUE r._rev FROM r WHERE r._rev IN ({revisionsList})";

            var existingRevisions = await Data.FromSqlAsync<string>(sql, datasetId);
            var missingRevisions = revisions[id].Except(existingRevisions).ToList();

            if (missingRevisions.Count != 0)
                result.Add(id, new MissingItems(missingRevisions));
        }

        return result;
    }

public void Map(IEndpointRouteBuilder endpoints)
    {
        var baseUrl = $"/data";
        var group = endpoints.MapGroup(baseUrl);

        group.MapGet("/db/{partitionKey}", GetDb);
        group.MapPost("/db/{datasetId}/_revs_diff", GetRevsDiff);
    }
}
