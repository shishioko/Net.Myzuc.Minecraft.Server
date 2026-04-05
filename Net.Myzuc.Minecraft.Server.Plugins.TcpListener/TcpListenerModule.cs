using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Net.Myzuc.Minecraft.Server.Networking;
using Net.Myzuc.Minecraft.Server.Objects.Attributes;
using Net.Myzuc.Minecraft.Server.Resources;

namespace Net.Myzuc.Minecraft.Server.Plugins.TcpListener
{
    public static class TcpListenerModule
    {
        internal static readonly Logger Logger = LogManager.GetLogger(Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty);
        private static readonly JsonConfiguration<TcpListenerConfiguration> Config = new("MMEModule.TcpListener.Endpoints");
        [ModuleInitializer]
        private static async Task InitializeAsync()
        {
            await Config.LoadAsync();
            Server.OnStart += (sender, args) =>
            {
                try
                {
                    foreach (IPEndPoint endpoint in Config.Value.Endpoints)
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