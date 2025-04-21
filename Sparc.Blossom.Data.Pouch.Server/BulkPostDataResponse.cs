using System.Text.Json;

namespace Sparc.Blossom.Data.Pouch.Server
{
    public class BulkPostDataResponse
    {
        public BulkPostDataResponse(dynamic data)
        {
            if (data is JsonElement jsonElement)
            {
                Id = jsonElement.GetProperty("_id").GetString();
                Rev = jsonElement.GetProperty("_rev").GetString();
            }
            else
            {
                Id = data._id;
                Rev = data._rev;
            }
        }

        public string Id { get; set; }
        public string Rev { get; set; }
        public bool Ok { get; set; }
        public string Error { get; set; }
        public string Reason { get; set; }

        public void SetError(Exception e)
        {
            Ok = false;
            Error = e.GetType().Name;
            Reason = e.InnerException?.Message ?? e.Message;
        }
    }
}
