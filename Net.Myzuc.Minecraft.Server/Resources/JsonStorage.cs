using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Net.Myzuc.Minecraft.Server.Resources
{
    public sealed class JsonStorage<T> : Storage<T> where T : class, new()
    {
        public static JsonSerializerOptions DefaulttJsonSerializerOptions => new(JsonSerializerDefaults.Strict)
        {

            PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,

            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,

            IgnoreReadOnlyProperties = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNameCaseInsensitive = false,
            AllowTrailingCommas = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,

            WriteIndented = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        };
        private JsonSerializerOptions JsonSerializerOptions { get; }
        public JsonStorage(string identifier, JsonSerializerOptions? jsonSerializerOptions = null) : base(identifier)
        {
            JsonSerializerOptions = jsonSerializerOptions ?? DefaulttJsonSerializerOptions;
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