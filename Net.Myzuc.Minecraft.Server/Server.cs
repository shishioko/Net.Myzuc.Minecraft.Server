using System.Data;
using System.Drawing;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Threading;
using Net.Myzuc.Minecraft.Common.ChatComponents;
using Net.Myzuc.Minecraft.Common.Data;
using Net.Myzuc.Minecraft.Common.Protocol;
using Net.Myzuc.Minecraft.Server.Clients;
using Net.Myzuc.Minecraft.Server.Extensions;
using Net.Myzuc.Minecraft.Server.Objects.Events;
using Net.Myzuc.Minecraft.Server.Resources;
using NLog;

namespace Net.Myzuc.Minecraft.Server
{
    public static class Server
    {
        public static Logger Logger => LogManager.GetLogger(Assembly.GetCallingAssembly().GetName().Name ?? string.Empty);
        internal static readonly JsonConfiguration<ServerConfiguration> Config = new("Net.Myzuc.Minecraft.Server:Configuration");
        public static event AsyncEventHandler OnStart = (sender, args) => Task.CompletedTask;
        public static event AsyncEventHandler OnStop = (sender, args) => Task.CompletedTask;
        public static event AsyncEventHandler<ConnectedEventArgs> OnConnected = (sender, args) => Task.CompletedTask;
        internal static async Task Main()
        {
            try
            {
                Logger.Info("Starting server...");
                await Config.LoadAsync();
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


                OnConnected += async (sender, args) =>
                {
                    args.Client.OnProtocolStageChange += async (sender, args) =>
                    {
                        if (args.Client is LoginClient loginClient)
                        {
                            loginClient.OnStart += async (sender, args) =>
                            {
                                _ = Task.Run(
                                    async () =>
                                    {
                                        await loginClient.SetCompressionThesholdAsync(64);
                                        GameProfile? profile = await loginClient.EncryptAsync(true);
                                        profile ??= new(Guid.Empty, "Unknown");
                                        ChatComponent chat = new TextChatComponent($"{profile.Name} ist gebannt trust trust")
                                        {
                                            Color = Color.MidnightBlue,
                                            ShadowColor = Color.CornflowerBlue,
                                            Italic = true,
                                            Children = [
                                                new TextChatComponent(" ["),
                                                new PlayerObjectChatComponent(new RenderProfile(profile))
                                                {
                                                    Color = Color.White,
                                                    ShadowColor = Color.Transparent,
                                                },
                                                new TextChatComponent("], ["),
                                                new PlayerObjectChatComponent(new RenderProfile("simon_dersimon"))
                                                {
                                                    Color = Color.White,
                                                    ShadowColor = Color.Transparent,
                                                },
                                                new TextChatComponent("]")
                                            ]
                                        };
                                        JsonSerializerOptions so = new()
                                        {
                                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                                        };
                                        string chatjson = JsonSerializer.Serialize(chat, so);
                                        ChatComponent chat2 = JsonSerializer.Deserialize<ChatComponent>(chatjson, so) ?? throw new NoNullAllowedException();
                                        await loginClient.DisconnectAsync(chat2);
                                    }
                                );
                            };
                        }
                    };
                };
                
                
                await OnStart.InvokeAsync(null, EventArgs.Empty);
                Logger.Info("Started server.");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Error while starting Server: {ex}");
                await StopAsync(false);
            }
            await Task.Delay(-1);
        }
        public static async Task StopAsync(bool success)
        {
            try
            {
                Logger.Info("Stopping server...");
                await OnStop.InvokeAsync(null, EventArgs.Empty);
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
                socket.ReceiveTimeout = Config.Value.Timeout;
                socket.SendTimeout = Config.Value.Timeout;
                await using Connection connection = new(socket, true);
                Client? client = new HandshakeClient(connection);
                await OnConnected.InvokeAsync<ConnectedEventArgs>(null, new((client as HandshakeClient)!));
                try
                {
                    while (client is not null)
                    {
                        Client? newClient = await client.ListenAsync();
                        await client.DisposeAsync();
                        client = newClient;
                    }
                }
                finally
                {
                    if (client is not null) await client.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error while handling connection from {socket.RemoteEndPoint}: {ex}");
            }
        }
    }
}