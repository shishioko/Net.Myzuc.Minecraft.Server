using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Net.Myzuc.Minecraft.Server.Objects.JsonConverters;

namespace Net.Myzuc.Minecraft.Server.Resources
{
    public sealed class JsonConfiguration<T> : Configuration<T> where T : class, new()
    {
        private JsonSerializerOptions JsonSerializerOptions { get; }
        public JsonConfiguration(string identifier, JsonSerializerOptions? jsonSerializerOptions = null) : base(identifier)
        {
            JsonSerializerOptions = new(jsonSerializerOptions ?? new(JsonSerializerDefaults.General))
            {
                Converters =
                {
                    new IPEndPointConverter()
                },
                PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
            
                NumberHandling =  JsonNumberHandling.AllowReadingFromString |  JsonNumberHandling.AllowNamedFloatingPointLiterals,
            
                IncludeFields = true,
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = false,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                UnknownTypeHandling =  JsonUnknownTypeHandling.JsonElement,
            
                WriteIndented = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            };
        }
        public override T Deserialize(byte[] data)
        {
            string content = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(content, JsonSerializerOptions) ?? throw new InvalidDataException("Configuration file is empty!");
        }
        public override byte[] Serialize(T data)
        {
            string content = JsonSerializer.Serialize(data, JsonSerializerOptions);
            return Encoding.UTF8.GetBytes(content);
        }
        protected override string GetPath()
        {
            return $"{base.GetPath()}.json";
        }
    }
}