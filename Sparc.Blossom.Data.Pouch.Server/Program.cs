using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();


var cosmosClient = new CosmosClient(builder.Configuration.GetConnectionString("CosmosDB"));
var databaseResponse = cosmosClient.CreateDatabaseIfNotExistsAsync("Sparc2").GetAwaiter().GetResult();
var database = databaseResponse.Database;
var containerResponse = database.CreateContainerIfNotExistsAsync(
    id: "projectideas",
    partitionKeyPath: "/id",
    throughput: 400
).GetAwaiter().GetResult();

builder.Services.AddSingleton(containerResponse.Container);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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