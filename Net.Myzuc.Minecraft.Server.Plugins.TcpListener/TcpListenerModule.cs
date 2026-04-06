using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Net.Myzuc.Minecraft.Server.Objects.Attributes;
using Net.Myzuc.Minecraft.Server.Resources;
using NLog;

namespace Net.Myzuc.Minecraft.Server.Plugins.TcpListener
{
    public static class TcpListenerModule
    {
        internal static readonly Logger Logger = LogManager.GetLogger(Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty);
        private static readonly JsonConfiguration<TcpListenerConfiguration> Config = new("MMEModule.TcpListener.Configuration");
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
                    Logger.Warn($"Error while initializing: {ex}");
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
                Logger.Debug($"Listening on {endpoint}.");
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Socket client = await socket.AcceptAsync(cancellationToken);
                    Logger.Debug($"Registering connection from {endpoint}.");
                    _ = Server.HandleConnectionAsync(client);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error while listening on {endpoint}: {ex}");
            }
        }
    }
}