using MessagePack;
using Sparc.Blossom.Data;
using System.Reflection;

public static class DynamicEntityLoader
{
    private static readonly Dictionary<string, Type> typeRegistry = new();

    public static void LoadTypesFromAssembly(string assemblyPath)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        foreach (var type in assembly.GetTypes())
        {
            if (typeof(BlossomEntity).IsAssignableFrom(type) && !type.IsAbstract)
            {
                typeRegistry[type.Name] = type;
            }
        }
    }

    public static object CreateEntity(string typeName, byte[] data)
    {
        if (typeRegistry.TryGetValue(typeName, out var type))
        {
            return MessagePackSerializer.Deserialize(type, data);
        }

        throw new InvalidOperationException($"Type {typeName} is not registered.");
    }
}
