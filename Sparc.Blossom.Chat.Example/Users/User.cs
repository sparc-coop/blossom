using Sparc.Blossom;
using System.ComponentModel.DataAnnotations;

namespace Sparc.MCN.Users;

public class User : BlossomEntity<string>
{
    [Required(ErrorMessage = "FirstName is required")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "LastName is required")]
    public string LastName { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public Contact Contact { get; set; }
    public DateTime DateModified { get; set; }

    public User(string firstName, string lastName, Contact contact, string? prefix, string? suffix) : base(Guid.NewGuid().ToString())
    {
        FirstName = firstName;
        LastName = lastName;
        Prefix = prefix;
        Suffix = suffix;
        Contact = contact;
        DateModified = DateTime.Now;
    }
}