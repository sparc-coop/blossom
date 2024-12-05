using Sparc.Blossom.Data;
using System.Reflection;

namespace Sparc.Blossom;

public static class AssemblyExtensions
{
    public static IEnumerable<Type> GetTypes<T>(this Assembly assembly)
        => assembly.GetTypes().Where(x => typeof(T).IsAssignableFrom(x));

    public static IEnumerable<Type> GetDerivedTypes(this Assembly assembly, Type baseType)
        => assembly.GetTypes().Where(x =>
            (baseType.IsGenericType && x.BaseType?.IsGenericType == true && x.BaseType.GetGenericTypeDefinition() == baseType)
            || x.BaseType == baseType);

    public static IEnumerable<Type> GetEntities(this Assembly assembly)
        => assembly.GetDerivedTypes(typeof(BlossomEntity<>));

    public static IEnumerable<Type> GetAggregates(this Assembly assembly)
        => assembly.GetTypes().Where(x => typeof(IBlossomAggregate).IsAssignableFrom(x));

    public static Type? FindType(this AppDomain domain, string typeName)
    {
        foreach (var assembly in domain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }
}
