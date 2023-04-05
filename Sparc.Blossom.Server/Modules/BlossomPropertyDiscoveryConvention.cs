using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
#pragma warning disable EF1001 // Internal EF Core API usage.
        foreach (var propertyInfo in entityType.GetRuntimeProperties().Values.Where(x => x.CanRead && x.CanWrite && x.SetMethod?.IsAssembly == true))
        {
            entityTypeBuilder.Property(propertyInfo);
        }
#pragma warning restore EF1001 // Internal EF Core API usage.
    }
}
