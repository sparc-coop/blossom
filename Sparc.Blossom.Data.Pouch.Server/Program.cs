using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Sparc.Blossom;
using Sparc.Blossom.Data.Pouch;
using Sparc.Blossom.Data.Pouch.Server;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCosmos<CosmosContext>(builder.Configuration.GetConnectionString("CosmosDb"), builder.Configuration.GetValue<string>("CosmosDb:Database"));


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7022")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetIsOriginAllowed((string x) => true)
              .AllowCredentials();
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
        var item = await container.ReadItemAsync<ReplicationLog>(id, new PartitionKey(id));
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

app.MapPost("/db/{partitionKey}/_all_docs", async (string partitionKey, [FromServices] Container container) =>
{
    try
    {
        // Query all documents from the container
        var query = await container.FromSqlAsync(partitionKey, "SELECT * FROM c");

        // Map the results to the response format
        var response = new GetAllDataResponse(query);

        // Return the response
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        // Log the error and return a problem response
        Console.WriteLine($"Error: {ex.Message}");
        return Results.Problem("An error occurred while retrieving all documents.");
    }
});

app.MapPost("/db/{partitionKey}/_bulk_docs", async (string partitionKey, [FromBody] BulkPostDataRequest request, [FromServices] Container container) =>
{
    var results = new List<BulkPostDataResponse>();

    foreach (var doc in request.Docs)
    {
        results.Add(await PostRevision(partitionKey, doc, container));
    }

    return Results.Ok(results);
});

app.MapGet("/db/{partitionKey}/{id}", async (string partitionKey, string id, [FromServices] Container container) =>
{
    try
    {
        // Attempt to read the item from Cosmos DB
        var item = await container.ReadItemAsync<dynamic>(id, new PartitionKey(partitionKey));
        return Results.Ok(item.Resource);
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        // Return a "not found" response if the item does not exist
        return Results.NotFound();
    }
});

app.MapPost("/db/{partitionKey}/_bulk_get", async (string partitionKey, [FromBody] GetSpecificRevisionsRequest request, [FromServices] Container container) =>
{
    var data = new List<dynamic>();

    foreach (var doc in request.Docs)
    {
        try
        {
            // Attempt to read the specific revision of the document
            var result = await container.ReadItemAsync<dynamic>($"{doc.Id}-{doc.Rev}", new PartitionKey(partitionKey));
            data.Add(result.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Ignore not found errors
        }
    }

    // Map the results to the response format
    var response = GetSpecificRevisionsResponse.ToBulkGetResponse(data);

    // Return the response
    return Results.Ok(response);
});

app.MapPut("/db/{partitionKey}/{id}", async (string partitionKey, string id, [FromBody] dynamic datum, [FromServices] Container container) =>
{
    var result = new SaveDatumRevisionResponse(datum);

    try
    {
        // Set additional properties for the document
        datum._db = partitionKey;
        datum.id = $"{datum._id}-{datum._rev}";

        // Insert or update the document in the Cosmos DB container
        await container.CreateItemAsync(datum, new PartitionKey(partitionKey));

        // Mark the operation as successful
        result.Ok = true;
    }
    catch (Exception e)
    {
        // Handle errors and set error details in the response
        result.SetError(e);
    }

    // Return the response
    return Results.Ok(result);
});

app.MapMethods("/db/{partitionKey}", new[] { "HEAD" }, async (string partitionKey, [FromServices] Container container) =>
{
    try
    {
        // Attempt to read the item from Cosmos DB
        await container.ReadItemAsync<dynamic>(partitionKey, new PartitionKey(partitionKey));
        return Results.Ok(); // Return 200 OK if the dataset exists
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        // Return 404 Not Found if the dataset does not exist
        return Results.NotFound();
    }
});

app.MapPut("/db/{partitionKey}", (string partitionKey) =>
{
    // In Cosmos DB, partition keys don't need to be created in advance.
    // Simply return a success response.
    var response = new Dictionary<string, bool>
    {
        { "Ok", true }
    };

    return Results.Ok(response);
});

app.MapMethods("/db", new[] { "GET", "POST" }, () =>
{
    var response = new GetServerMetadataResponse(
        "Welcome to the Cosmos CouchDB API",
        "85fb71bf700c17267fef77535820e371",
        new ServerMetadataVendor("Sparc Cooperative", "2.0.1"),
        "2.0.1"
    );

    return Results.Ok(response);
});

static async Task<BulkPostDataResponse> PostRevision(string partitionKey, dynamic doc, Container container)
{
    var result = new BulkPostDataResponse(doc);

    try
    {
        var mutableDoc = JsonElementToDictionary(doc);

        mutableDoc["_db"] = partitionKey;
        mutableDoc["PartitionKey"] = mutableDoc["id"].ToString();
        //mutableDoc["id"] = $"{mutableDoc["_id"]}-{mutableDoc["_rev"]}";
        //mutableDoc["id"] = $"{mutableDoc["_id"]}-{mutableDoc["_rev"]}"; //but this will be different from _id?

        //var jsonString = JsonSerializer.Serialize(mutableDoc);
        //var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        await container.CreateItemAsync(mutableDoc, new PartitionKey(mutableDoc["id"].ToString()));

        result.Ok = true;
    }
    catch (Exception e)
    {
        // Handle errors and set error details in the response
        result.SetError(e);
    }

    return result;
}

static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
{
    var dict = new Dictionary<string, object>();

    foreach (var property in element.EnumerateObject())
    {
        dict[property.Name] = ConvertJsonValue(property.Value);
    }

    return dict;
}

static object ConvertJsonValue(JsonElement value)
{
    return value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.TryGetInt64(out var l) ? l : value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null!,
        JsonValueKind.Object => JsonElementToDictionary(value),
        JsonValueKind.Array => value.EnumerateArray().Select(ConvertJsonValue).ToList(),
        _ => value.GetRawText()
    };
}

app.Run();
