using System.Net;

namespace Net.Myzuc.Minecraft.Server.Plugins.TcpListener
{
    public record TcpListenerConfiguration
    {
        public List<IPEndPoint> Endpoints =
        [
            new(IPAddress.Any, 25565)
        ];
    }
}