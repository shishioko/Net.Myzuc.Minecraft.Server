using System.Net;

namespace Net.Myzuc.Minecraft.Server.Plugins.TcpListener
{
    public sealed record TcpListenerConfiguration
    {
        public IReadOnlyList<IPEndPoint> Endpoints { get; init; } =
        [
            new(IPAddress.Any, 25565)
        ];
    }
}