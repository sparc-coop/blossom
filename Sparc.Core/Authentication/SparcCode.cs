namespace Sparc.Blossom.Authentication;

public class SparcCode(string code, int? remainingSeconds = null)
{
    public SparcCode() : this("")
    { }
    
    public string Code { get; set; } = code.Replace("totp:", "");
    public int RemainingSeconds { get; set; } = remainingSeconds ?? 0;

    public override string ToString()
    {
        var hyphenLoc = (int)Math.Ceiling(Code.Length / 2.0);
        return $"{Code.Substring(0, hyphenLoc)}-{Code.Substring(hyphenLoc)}";
    }

}