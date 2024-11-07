using Sparc.Blossom.Api;
using Sparc.Blossom.Data;
using System.Reflection;

namespace Sparc.Blossom;

public static class AssemblyExtensions
{
    public static IEnumerable<Type> GetDerivedTypes(this Assembly assembly, Type baseType)
        => assembly.GetTypes().Where(x =>
            (baseType.IsGenericType && x.BaseType?.IsGenericType == true && x.BaseType.GetGenericTypeDefinition() == baseType)
            || x.BaseType == baseType);

    public static IEnumerable<Type> GetEntities(this Assembly assembly)
        => assembly.GetDerivedTypes(typeof(BlossomEntity<>));
}
