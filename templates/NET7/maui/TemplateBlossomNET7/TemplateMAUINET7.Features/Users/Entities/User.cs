using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace TemplateWebNET7.Features.Users.Entities
{
    public class User : BlossomUser
    {
        public User()
        {
            Id = Guid.NewGuid().ToString();
            UserId = Id;
            DateCreated = DateTime.UtcNow;
            DateModified = DateTime.UtcNow;
        }

        public User(string email) : this()
        {
            Email = email;
        }

        public User(string azureId, string email) : this(email)
        {
            AzureB2CId = azureId;
        }

        public string UserId { get { return Id; } set { Id = value; } }
        private string? _email;
        public string? Email
        {
            get { return _email; }
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _email = null;
                    return;
                }

                _email = value.Trim().ToLower();
            }
        }

        public DateTime DateCreated { get; private set; }
        public DateTime DateModified { get; private set; }
        public string? SlackTeamId { get; private set; }
        public string? SlackUserId { get; private set; }
        public string? AzureB2CId { get; private set; }
        public string? PhoneNumber { get; private set; }

       
        protected override void RegisterClaims()
        {
            AddClaim(ClaimTypes.Email, Email);
            AddClaim("sub", AzureB2CId);
        }
    }

}
