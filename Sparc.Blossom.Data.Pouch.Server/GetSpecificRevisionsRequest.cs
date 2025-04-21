namespace Sparc.Blossom.Data.Pouch.Server
{
    public class GetSpecificRevisionsRequest
    {
        public string PartitionKey { get; set; }
        public List<BulkGetDatum> Docs { get; set; }

        public class BulkGetDatum
        {
            public string Id { get; set; }
            public string Rev { get; set; }
        }
    }

}
