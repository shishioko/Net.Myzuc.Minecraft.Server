using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Loader;
using Net.Myzuc.Minecraft.Common.Protocol;
using Net.Myzuc.Minecraft.Common.Protocol.Packets;
using Net.Myzuc.Minecraft.Server.Extensions;
using NLog;

namespace Net.Myzuc.Minecraft.Server
{
    public static class Server
    {
        internal static readonly Logger Logger = LogManager.GetLogger(Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty);
        public static event EventHandler OnStart = (sender, args) => { };
        public static event EventHandler OnStop = (sender, args) => { };
        internal static async Task Main(string[] args)
        {
            try
            {
                Logger.Info("Starting server...");
                Logger.Debug("Loading libraries...");
                FileInfo[] files = Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libraries")).GetFiles("*.dll", SearchOption.AllDirectories);
                IEnumerable<Assembly> assemblies = (await Task.WhenAll(
                    files.Select(
                        async (file) =>
                        {
                            try
                            {
                                Assembly assembly = await AssemblyLoadContext.Default.LoadFromAssemblyPathAsync(file.FullName);
                                Logger.Debug($"Loaded library \"{assembly.GetName().GetVersionedName()}\" from \"{file.FullName}\".");
                                return assembly;
                            }
                            catch (Exception ex)
                            {
                                Logger.Debug($"Error while loading library from \"{file.FullName}\": {ex}");
                                return null;
                            }
                        }
                    )
                )).Where(assembly => assembly != null)!;
                Logger.Debug("Loaded libraries.");
                Logger.Debug("Initializing modules...");
                await Task.WhenAll(
                    assemblies.SelectMany(assembly =>
                        assembly.GetTypes().SelectMany(type =>
                            type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(method => (method.ReturnType == typeof(void) || method.ReturnType == typeof(Task)) && method.GetParameters().Length == 0).Select(
                                async (method) =>
                                {
                                    try
                                    {
                                        if (method.Invoke(null, null) is Task task) await task;
                                        Logger.Debug($"Initialized module \"{type.Name}\" from \"{assembly.GetName().GetVersionedName()}\" using \"{method.Name}\".");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Debug($"Error while initializing module \"{type.Name}\" from \"{assembly.GetName().GetVersionedName()}\" using \"{method.Name}\": {ex}");
                                    }
                                }
                            )
                        )
                    )
                );
                Logger.Debug("Initialized modules.");
                OnStart(null, EventArgs.Empty);
                Logger.Info("Started server.");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error while starting Server: {ex}");
                Stop(false);
            }
            await Task.Delay(-1);
        }
        public static void Stop(bool success)
        {
            try
            {
                OnStop(null, EventArgs.Empty);
                LogManager.Flush();
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error while stopping Server: {ex}");
            }
            Environment.Exit(success ? 0 : 1);
        }
        public static async Task HandleConnectionAsync(Socket socket)
        {
            try
            {
                using Connection connection = new Connection(socket);
                HandshakePacket handshake = await connection.ReadAsync<HandshakePacket>();
                switch (connection.ProtocolStage)
                {
                    case ProtocolStage.Status:
                    {
                        break;
                    }
                    case ProtocolStage.Login:
                    {
                        break;
                    }
                    default:
                    {
                        throw new ProtocolViolationException("Unknown Intent!");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error while handling connection from {socket.RemoteEndPoint}: {ex}");
            }
        }
    }
}