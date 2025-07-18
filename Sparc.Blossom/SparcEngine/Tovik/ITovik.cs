using Refit;
using Sparc.Blossom.Authentication;

namespace Sparc.Engine.Tovik;

public interface ITovik
{
    [Get("/tovik/languages")]
    Task<IEnumerable<Language>> GetLanguages();

    [Post("/auth/user-languages")]
    Task<BlossomUser> AddUserLanguage([Body] Language language);
}
