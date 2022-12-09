namespace Sparc.Blossom.Authentication;

public interface ITokenProvider
{
    string? Token { get; set; }
}