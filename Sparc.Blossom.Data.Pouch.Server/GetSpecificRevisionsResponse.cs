namespace Sparc.Blossom.Data.Pouch.Server
{
    public class GetSpecificRevisionsResponse
    {
        public List<Result> results { get; set; }

        public static GetSpecificRevisionsResponse ToBulkGetResponse(List<dynamic> data)
        {
            List<Result> results = new();

            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }

                Result result = new()
                {
                    id = item._id,
                    docs = new List<Doc>()
                };

                Doc doc = new()
                {
                    ok = item
                };

                result.docs.Add(doc);

                results.Add(result);
            }

            return new GetSpecificRevisionsResponse
            {
                results = results
            };
        }
    }

    public class Result
    {
        public string id { get; set; }
        public List<Doc> docs { get; set; }
    }

    public class Doc
    {
        public dynamic ok { get; set; }
    }

}
