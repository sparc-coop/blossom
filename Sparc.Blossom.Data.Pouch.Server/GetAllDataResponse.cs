namespace Sparc.Blossom.Data.Pouch.Server
{
    public class GetAllDataResponse
    {
        public GetAllDataResponse(List<dynamic> data)
        {
            rows = DatumSummary.ToDatumSummary(data);
            offset = 0;
            total_rows = data.Count;
        }

        public int offset { get; set; }
        public List<DatumSummary> rows { get; set; }
        public int total_rows { get; set; }
    }
}
