using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Net.Myzuc.Minecraft.Server.Networking;
using Net.Myzuc.Minecraft.Server.Objects.Attributes;
using Net.Myzuc.Minecraft.Server.Resources;

namespace Net.Myzuc.Minecraft.Server.Modules.TcpListener
{
    public static class TcpListenerModule
    {
        private static readonly JsonConfiguration<ConcurrentBag<IPEndPoint>> Config = new("MMEModule.TcpListener.Endpoints");
        [ModuleInitializer]
        private static async Task InitializeAsync()
        {
            if (await Config.LoadAsync() is null)
            {
                Config.Value =
                [
                    new(IPAddress.Any, 25565)
                ];
                await Config.SaveAsync();
            }
            Engine.OnStart += (sender, args) =>
            {
                try
                {
                    foreach (IPEndPoint endpoint in Config.Value)
                    {
                        _ = ListenAsync(endpoint);
                    }
                }
                catch (Exception ex)
                {
                    Logs.Warning($"Error while initializing: {ex}");
                }
            };
        }
        public static async Task ListenAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                using Socket socket = new(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(endpoint);
                socket.Listen();
                Logs.Verbose($"Listening on {endpoint}.");
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Socket client = await socket.AcceptAsync(cancellationToken);
                    try
                    {
                        Connection connection = new(new NetworkStream(client, true), client.RemoteEndPoint);
                        Connection.RegisterConnection(connection);
                    }
                    catch (Exception ex)
                    {
                        Logs.Warning($"Error while handling connection from {client.RemoteEndPoint} on {endpoint}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.Warning($"Error while listening on {endpoint}: {ex}");
            }
        }
    }
}