using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Sparc.Blossom;
using Sparc.Blossom.Data.Pouch;
using Sparc.Blossom.Data.Pouch.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCosmos<CosmosContext>(builder.Configuration.GetConnectionString("CosmosDb"), builder.Configuration.GetValue<string>("CosmosDb:Database"));


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var settings = builder.Configuration.GetSection("CosmosDb").Get<CosmosPouchAdapterSettings>();
builder.Services.AddSingleton(_ => settings);
builder.Services.AddSingleton(_ => new CosmosClient(settings.Url, settings.Key).GetContainer(settings.Database, settings.SourceContainerName));
builder.Services.AddHostedService<SetUpdateSequence>();

builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();


app.MapPost("/api/db/sync/{partitionKey}", async (string partitionKey, Document doc, [FromServices]Container container) =>
{
    doc.LastModified = DateTime.UtcNow;
    await container.UpsertItemAsync(doc, new PartitionKey(doc.id));
    return Results.Ok(doc);
});

app.MapGet("/db/{partitionKey}", async (string partitionKey, [FromServices] Container container) =>
{
    try
    {
        // Query for document count
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) };
        var documentCount = await container.GetItemLinqQueryable<dynamic>(requestOptions: options).CountAsync();

        // Query for the last update sequence
        var query = "SELECT TOP 1 c._seq FROM c WHERE ISNULL(c._seq) = false ORDER BY c._seq DESC";
        var iterator = container.GetItemQueryIterator<dynamic>(query, requestOptions: options);
        var lastUpdateSequence = (await iterator.ReadNextAsync()).FirstOrDefault()?._seq;

        // Return the response
        return Results.Ok(new GetDatasetMetadataResponse(partitionKey, documentCount ?? 0, 0, lastUpdateSequence));
    }
    catch (Exception ex)
    {
        // Log the error and return a problem response
        Console.WriteLine($"Error: {ex.Message}");
        return Results.Problem("An error occurred while retrieving dataset metadata.");
    }
});

app.MapPost("/db/{datasetId}/_revs_diff", async (string datasetId, [FromBody] Dictionary<string, List<string>> revisions, [FromServices] Container container) =>
{
    var result = new Dictionary<string, MissingItems>();

    foreach (var id in revisions.Keys)
    {
        var revisionsList = string.Join(", ", revisions[id].Select(x => $"'{x}'"));
        var sql = $"SELECT VALUE r._rev FROM r WHERE r._rev IN ({revisionsList})";
        var iterator = container.GetItemQueryIterator<string>(
            new QueryDefinition(sql),
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(datasetId) }
        );

        var existingRevisions = await iterator.ReadNextAsync();
        var missingRevisions = revisions[id].Except(existingRevisions).ToList();

        if (missingRevisions.Any())
        {
            result.Add(id, new MissingItems(missingRevisions));
        }
    }

    return Results.Ok(result);
});

app.MapPost("/db/{partitionKey}/_changes", async (string partitionKey, [FromBody] GetChangesRequest request, [FromServices] Container container) =>
{
    try
    {
        // Build the SQL query
        var sql = "SELECT c._id, c._rev, c._seq FROM c WHERE IS_NULL(c._seq) = false";

        if (!string.IsNullOrEmpty(request.since) && request.since != "0")
            sql += $" AND c._seq > '{request.since}'";

        if (request.limit.HasValue)
            sql = sql.Replace("SELECT", $"SELECT TOP {request.limit.Value}");

        sql += " ORDER BY c._seq";

        // Execute the query
        var results = await container.FromSqlAsync(partitionKey, sql);

        // Determine the last sequence
        var last_seq = (results.LastOrDefault()?._seq ?? request.since) ?? "0";

        // Map the results to the response format
        var output = results
            .Select(x => new GetChangesResult(new List<GetChangesRev> { new GetChangesRev(x._rev) }, x._id, x._seq))
            .ToList();

        // Return the response
        return Results.Ok(new GetChangesResponse(last_seq, output));
    }
    catch (Exception ex)
    {
        // Log the error and return a problem response
        Console.WriteLine($"Error: {ex.Message}");
        return Results.Problem("An error occurred while retrieving changes.");
    }
});

app.MapGet("/db/{partitionKey}/_local/{id}", async (string partitionKey, string id, [FromServices] Container container) =>
{
    try
    {
        // Attempt to read the item from Cosmos DB
        var item = await container.ReadItemAsync<ReplicationLog>(id, new PartitionKey(partitionKey));
        return Results.Ok(item.Resource);
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        // Return a "not found" response if the item does not exist
        var dictionary = new Dictionary<string, string>
        {
            { "error", "not_found" },
            { "reason", "missing" }
        };

        return Results.NotFound(dictionary);
    }
});

app.MapPut("/db/{datasetId}/_local/{documentId}", async (string datasetId, string documentId, [FromBody] ReplicationLog log, [FromServices] IRepository<ReplicationLog> logs) =>
{
    try
    {
        // Set the log properties from the dataset and document IDs
        log.SetFromPouch(datasetId, documentId);

        // Update the log in the repository
        await logs.UpdateAsync(log);

        // Return the updated log
        return Results.Ok(log);
    }
    catch (Exception ex)
    {
        // Log the error and return a problem response
        Console.WriteLine($"Error: {ex.Message}");
        return Results.Problem("An error occurred while saving the checkpoint.");
    }
});

app.Run();

public class Document
{
    public string id { get; set; }
    public string UserId { get; set; }
    public string Type { get; set; }
    public dynamic Data { get; set; }
    public DateTime LastModified { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public DateTime DateCreated { get; set; }
    public List<string> FileUrls { get; set; } = new();
}