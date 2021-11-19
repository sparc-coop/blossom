using Sparc.Core;

namespace Sparc.Database.Cosmos
{
    public class CosmosDbRoot : Root<string>
    {
        public string? Discriminator { get; set; }
        public string? PartitionKey { get; set; }
    }
}