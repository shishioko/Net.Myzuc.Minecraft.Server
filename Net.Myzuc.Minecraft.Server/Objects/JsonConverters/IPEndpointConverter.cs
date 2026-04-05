using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Net.Myzuc.Minecraft.Server.Objects.JsonConverters
{
    public class IPEndPointConverter : JsonConverter<IPEndPoint>
    {
        public override IPEndPoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(IPEndPoint)) return null;
            string? endpoint = reader.GetString();
            return endpoint is null ? null : IPEndPoint.Parse(endpoint);
        }
        public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}