using Bogus;
using Sparc.Blossom;

namespace Sparc.Engine;

public class SparcDomain(string domain) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string Domain { get; set; } = domain;
    public List<SparcProduct> Products { get; set; } = [];
    public int TotalUsage = new Random().Next(1, 100000);
    public List<string> Exemptions{ get; set; } = new();
    public DateTime? DateConnected { get; set; }
    //public int TotalUsage => Products.Sum(p => p.UsageMeter);

    public bool HasProduct(string policyName) => policyName == "Auth" || Products.Any(p => p.ProductId == policyName);

    public static IEnumerable<SparcDomain> Generate(int qty)
    {
        var faker = new Faker<SparcDomain>()
            .CustomInstantiator(f => new SparcDomain(
                f.Lorem.Sentence(3)
            ));

        return faker.Generate(qty);
    }
}