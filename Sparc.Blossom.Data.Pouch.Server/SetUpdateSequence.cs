using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Sparc.Blossom.Data.Pouch.Server
{
    public class SetUpdateSequence : IHostedService

    {
        public SetUpdateSequence(IServiceProvider serviceProvider, CosmosPouchAdapterSettings settings, Container container)
        {
            ServiceProvider = serviceProvider;
            Settings = settings;
            Container = container;
            Database = new CosmosClient(settings.Url, settings.Key).GetDatabase(settings.Database);
        }

        public IServiceProvider ServiceProvider { get; }
        public CosmosPouchAdapterSettings Settings { get; }
        public Container Container { get; private set;  }
        public Microsoft.Azure.Cosmos.Database Database { get; }
        public ChangeFeedProcessor ChangeFeedProcessor { get; private set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Ensure the database exists
            var databaseResponse = await new CosmosClient(Settings.Url, Settings.Key)
                .CreateDatabaseIfNotExistsAsync(Settings.Database);
            var database = databaseResponse.Database;

            // Ensure the source container exists
            var containerResponse = await database.CreateContainerIfNotExistsAsync(
                id: Settings.SourceContainerName,
                partitionKeyPath: "/DatasetId", 
                throughput: 400
            );
            Container = containerResponse.Container;

            // Ensure the lease container exists
            var leaseContainerResponse = await database.CreateContainerIfNotExistsAsync(
                id: Settings.LeaseContainerName,
                partitionKeyPath: "/id", 
                throughput: 400
            );
            var leaseContainer = leaseContainerResponse.Container;



            // Build the Change Feed Processor
            ChangeFeedProcessor = Container.GetChangeFeedProcessorBuilder<dynamic>("RevisionProcessor", SetUpdateSequenceAsync)
                .WithInstanceName(Settings.InstanceName)
                .WithLeaseContainer(leaseContainer)
                .Build();

            await ChangeFeedProcessor.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task SetUpdateSequenceAsync(IReadOnlyCollection<dynamic> revisions, CancellationToken cancellationToken)
        {
            foreach (var revision in revisions.Where(x => x.Discriminator == "Revision"))
            {
                if (revision.UpdateSequence == null && revision._lsn != null)
                {
                    var updateSequence = revision._lsn;
                    revision.UpdateSequence = updateSequence;
                    await Container.UpsertItemAsync(revision);

                    var datum = ToDatum(revision);
                    datum.UpdateSequence = updateSequence;
                    await Container.UpsertItemAsync(datum);

                    var dataset = await Container.ReadItemAsync(datum.DatasetId, new PartitionKey(datum.PartitionKey));
                    if (dataset != null)
                    {
                        //dataset.DocumentCount = await Container.GetItemQueryIterator<int>(new QueryDefinition(;
                        dataset.UpdateSequence = updateSequence;
                        await Container.UpsertItemAsync(dataset);
                    }
                }
            }
        }
        public static dynamic ToDatum(dynamic revision)
        {
            var serializedParent = JsonConvert.SerializeObject(revision);
            // hack
            serializedParent = serializedParent.Replace("\"Discriminator\":\"Revision\"", "\"Discriminator\":\"Datum\"");
            var datum = JsonConvert.DeserializeObject(serializedParent);
            datum.Id = revision._id;
            return datum;
        }
    }
}
