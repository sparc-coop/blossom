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

    public static Dictionary<Type, Type> GetDtos(this Assembly assembly)
    {
        var entities = assembly.GetEntities();

        var dtos = assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>))
           .Select(x => x.BaseType!.GetGenericArguments().First())
           .Distinct();

        return dtos
            .ToDictionary(x => x, x => entities.FirstOrDefault(y => y.Name == x.Name))
            .Where(x => x.Value != null)
            .ToDictionary(x => x.Key, x => x.Value!);
    }

    public static Type? GetAggregateProxy(this Assembly assembly, Type entityType)
    => assembly.GetDerivedTypes(typeof(BlossomAggregateProxy<>).MakeGenericType(entityType)).FirstOrDefault();

    public static IEnumerable<MethodInfo> GetMyMethods(this Type type)
        => type.GetMethods().Where(x => x.DeclaringType == type && !x.IsSpecialName);
}
