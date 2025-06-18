using MediatR;
using Microsoft.Azure.Cosmos;

namespace Sparc.Blossom.Data;

public class PouchRepository(CosmosDbSimpleClient<PouchDatum> client, IMediator mediator) 
    : CosmosDbSimpleRepository<PouchDatum>(client, mediator)
{
    public async Task UpsertAsync(dynamic item, string? partitionKey = null)
    {
        


        var pk = partitionKey != null ? NewSparcHierarchicalPartitionKey(partitionKey) : GetPartitionKey(item);

        await Container.UpsertItemAsync(item, pk);
    }

    public async Task UpsertAsync(IEnumerable<dynamic> items, string? partitionKey = null)
    {
        foreach (var item in items)
        {
            await UpsertAsync(item, partitionKey);
        }
    }
}
