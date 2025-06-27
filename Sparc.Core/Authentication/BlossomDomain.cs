
namespace Sparc.Blossom.Authentication;

public class BlossomDomain(string domain) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string Domain { get; set; } = domain;
    public List<ProductKey> Products { get; set; } = [];

    public bool HasProduct(string policyName) => policyName == "Auth" || Products.Any(p => p.ProductId == policyName);
}
