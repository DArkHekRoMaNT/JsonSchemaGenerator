using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace JsonSchemaGenerator
{
    public static class Constants
    {
        public static string[] ExcludedTypeNames = new string[] {
            nameof(JsonObject)
        };
    }
}