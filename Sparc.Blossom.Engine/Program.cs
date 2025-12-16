using Anthropic.SDK;
using Anthropic.SDK.Batches;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using OpenAI;
using Scalar.AspNetCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Sparc.Blossom.Engine;
using Sparc.Blossom.Plugins.Slack;
using Sparc.Blossom.Realtime;
using Sparc.Blossom.Spaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<FriendlyId>();

builder.Services.AddCosmos<SparcEngineContext>(builder.Configuration.GetConnectionString("Cosmos")!, builder.Environment.IsDevelopment() ? "sparc-dev" : "sparc", ServiceLifetime.Scoped);
builder.Services.AddAzureStorage(builder.Configuration.GetConnectionString("Storage")!);

builder.AddSparcAuthentication<BlossomUser>();
builder.AddSparcBilling();
builder.AddSparcSpaces();
builder.Services.AddScoped(_ => new OpenAIClient(builder.Configuration.GetConnectionString("OpenAI")!));

builder.Services.AddSlackIntegration();

Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", builder.Configuration.GetConnectionString("Anthropic"));
builder.Services.AddHttpClient<AnthropicClient>().AddStandardResilienceHandler();

builder.AddSparcContent();
builder.Services.AddBlossomService<ProcessContent>();

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblyContaining<Program>();
    options.RegisterServicesFromAssemblyContaining<BlossomEntityChanged>();
    options.NotificationPublisher = new TaskWhenAllPublisher();
    options.NotificationPublisherType = typeof(TaskWhenAllPublisher);
});

builder.Services.AddTwilio(builder.Configuration);

builder.Services.AddScoped<ICorsPolicyProvider, SparcEngineDomainPolicyProvider>();
builder.Services.AddCors();

builder.Services.AddHybridCache();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
});

var app = builder.Build();
app.MapStaticAssets();
app.UseSparcAuthentication<BlossomUser>();
app.UseSparcBilling();
app.UseSparcSpaces();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapGet("/aura/friendlyid", (FriendlyId friendlyId) => friendlyId.Create());
app.MapGet("/hi", () => "Hi from Sparc!");

app.MapGet("/slack/import", async (SlackIntegrationService slack, IRepository<BlossomPost> repo) =>
{
    var existingMessages = await repo.Query.Where(x => x.SpaceId == "slacktest").ToListAsync();
    if (existingMessages.Any())
        await repo.DeleteAsync(existingMessages);
    
    var channels = new List<SlackNet.Conversation>();
    var english = Language.Find("en-US");
    await foreach (var channelBatch in slack.GetChannelsAsync(1000))
    {
        await foreach (var messageBatch in slack.GetMessagesAsync(channelBatch.Select(x => x.Id), 10000))
        {
            var posts = messageBatch.Select(x => new BlossomPost("kuviocreative.com", "slacktest", english!, x.Text, new() { Avatar = new(x.User, x.User) }));
            await repo.AddAsync(posts);
        }
    }
});

app.MapGet("/slack/vectorize", async (IRepository<BlossomPost> repo, IEnumerable<ITranslator> translators, IRepository<BlossomVector> vectorRepo) =>
{
    var messages = await repo.Query.Where(x => x.Domain == "kuviocreative.com" && x.SpaceId == "slacktest").ToListAsync();
    var offset = 0;
    var batchSize = 1000;

    do
    {
        var batch = messages
                    .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                    //.OrderBy(x => x.Sequence)
                    .Skip(offset)
                    .Take(batchSize)
                    .ToList();

        var ids = batch.Select(x => x.Id).ToList();
        var existing = await vectorRepo.Query.Where(x => ids.Contains(x.TargetUrl)).Select(x => x.TargetUrl).ToListAsync();
        batch = batch.Where(x => !existing.Contains(x.Id)).ToList();
        if (batch.Count > 0)
        {
            var translator = translators.OfType<OpenAITranslator>().First();
            var vectors = await translator.VectorizeAsync(batch);
            await vectorRepo.AddAsync(vectors);
        }
        offset += batchSize;
    } while (offset < messages.Count());
});

using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<Contents>().Map(app);

foreach (var translator in scope.ServiceProvider.GetServices<ITranslator>())
    await translator.GetLanguagesAsync();

if (!string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Cognitive")))
{
    var translator = scope.ServiceProvider.GetRequiredService<Contents>();
    await translator.GetLanguagesAsync();
}
app.Run();
