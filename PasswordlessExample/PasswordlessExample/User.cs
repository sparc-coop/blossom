using Ardalis.Specification;
using Sparc.Blossom.Authentication;

namespace PasswordlessExample
{
    public class User : BlossomUser
    {
        //public string? Name { get; set; }
        public string? Email { get; set; }
        //public string ExternalId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = false;

    }
}
