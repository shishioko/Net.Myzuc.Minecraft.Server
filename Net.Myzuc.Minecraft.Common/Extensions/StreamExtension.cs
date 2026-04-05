using System.Text;
using Me.Shiokawaii.IO;

namespace Net.Myzuc.Minecraft.Server.Extensions
{
    public static class StreamExtension
    {
        public static async Task<string> ReadMinecraftStringAsync(this Stream stream)
        {
            byte[] buffer = await stream.ReadU8AAsync(await stream.ReadS32VAsync());
            return Encoding.UTF8.GetString(buffer);
        }
        public static string ReadMinecraftString(this Stream stream)
        {
            byte[] buffer = stream.ReadU8A(stream.ReadS32V());
            return Encoding.UTF8.GetString(buffer);
        }
        public static async Task WriteMinecraftStringAsync(this Stream stream, string data)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            await stream.WriteS32VAsync(buffer.Length);
            await stream.WriteU8AAsync(buffer);
        }
        public static void WriteMinecraftString(this Stream stream, string data)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            stream.WriteS32V(buffer.Length);
            stream.WriteU8A(buffer);
        }
    }
}