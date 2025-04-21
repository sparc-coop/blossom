using Newtonsoft.Json.Serialization;

namespace Sparc.Blossom.Data
{
    internal class CamelCaseIdContractResolver : CamelCasePropertyNamesContractResolver
    {
        internal CamelCaseIdContractResolver()
        {
            NamingStrategy = new CamelCaseIdNamingStrategy
            {
                ProcessDictionaryKeys = true,
                OverrideSpecifiedNames = true
            };
        }
    }

    internal class CamelCaseIdNamingStrategy : DefaultNamingStrategy
    {
        protected override string ResolvePropertyName(string name)
        {
            return name == "Id" ? "id" : base.ResolvePropertyName(name);
        }
    }
}