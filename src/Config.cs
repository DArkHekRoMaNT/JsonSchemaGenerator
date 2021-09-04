using NJsonSchema.Generation;

namespace JsonSchemaGenerator
{
    public class Config
    {
        public static Config Current { get; internal set; } = new Config();

        public ReferenceTypeNullHandling ReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
        public bool AllowAny = true;
    }
}