namespace Sparc.Blossom.Data.Pouch.Server
{
    public class ReplicationHistory
    {
        public long last_seq { get; set; }
        // todo: change type to date
        public string session_id { get; set; }
    }
}
