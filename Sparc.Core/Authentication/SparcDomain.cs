
using Sparc.Blossom;

namespace Sparc.Engine;

public class SparcDomain(string domain) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string Domain { get; set; } = domain;
    public List<SparcProduct> Products { get; set; } = [];

    public bool HasProduct(string policyName) => policyName == "Auth" || Products.Any(p => p.ProductId == policyName);
}
