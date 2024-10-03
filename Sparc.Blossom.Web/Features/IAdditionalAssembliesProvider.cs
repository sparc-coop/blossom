using System.Reflection;

namespace Sparc.Blossom.Server;

public class AdditionalAssembliesProvider
{
    public IEnumerable<Assembly> Assemblies { get; set; } = new List<Assembly>();
}

