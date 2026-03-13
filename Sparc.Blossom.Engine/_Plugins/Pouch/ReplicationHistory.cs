namespace Sparc.Blossom.Data.Pouch;

public class ReplicationHistory
{
    public long last_seq { get; set; }
    // todo: change type to date
    public string session_id { get; set; } = "";
}
