namespace Sparc.Blossom.Data.Pouch.Server
{
    public record SaveDatumRevisionResponse(bool Ok, string Error, string Reason)
    {
        public string Id { get; set; } = data._id;
        public string Rev { get; set; } = data._rev;

        public void SetError(Exception e)
        {
            Ok = false;
            Error = e.GetType().Name;
            Reason = e.InnerException?.Message ?? e.Message;
        }
    }

}
