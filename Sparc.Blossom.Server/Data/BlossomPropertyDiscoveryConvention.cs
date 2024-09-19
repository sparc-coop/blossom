using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Reflection;

namespace Sparc.Blossom;

public class BlossomPropertyDiscoveryConvention : IEntityTypeAddedConvention
{
    public void ProcessEntityTypeAdded(IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
     => Process(entityTypeBuilder);

    void Process(IConventionEntityTypeBuilder entityTypeBuilder)
    {
        var entityType = entityTypeBuilder.Metadata;
        var model = entityType.Model;
        foreach (var propertyInfo in entityType.ClrType.GetRuntimeProperties().Where(x => x.CanRead && x.CanWrite && x.SetMethod?.IsAssembly == true))
        {
            entityTypeBuilder.Property(propertyInfo);
        }
    }
}
