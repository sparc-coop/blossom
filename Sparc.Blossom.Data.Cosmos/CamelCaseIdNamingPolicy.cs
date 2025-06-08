using System.Text.Json;

namespace Sparc.Blossom.Data
{
    internal class CamelCaseIdNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name == "Id" ? "id" : name;
        }
    }
}