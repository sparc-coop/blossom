using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace Sparc.Blossom;

internal class BlossomAssemblyProvider(Type appType, Type defaultLayout)
{
    public Assembly AppAssembly { get; set; } = appType.Assembly;
    public Type DefaultLayout { get; set; } = defaultLayout;
    public Assembly[] AdditionalAssemblies => 
        AppAssembly.FullName == DefaultLayout.Assembly.FullName
        ? [GetType().Assembly]
        : [GetType().Assembly, DefaultLayout.Assembly];
} 

