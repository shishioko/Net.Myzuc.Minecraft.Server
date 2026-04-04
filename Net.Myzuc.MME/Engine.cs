using System.Reflection;
using System.Runtime.Loader;
using Net.Myzuc.MME.Extensions;

namespace Net.Myzuc.MME
{
    public static class Engine
    {
        public static string MinecraftVersionName { get; } = "1.21.11";
        public static int MinecraftVersionProtocol { get; } = 774;
        public static event EventHandler OnLoad = (sender, args) => { };
        public static event EventHandler OnUnload = (sender, args) => { };
        internal static async Task Main(string[] args)
        {
            Logs.Info("Starting MME...");
            IEnumerable<Assembly> assemblies = [];
            try
            {
                Logs.Info("Loading libraries...");
                FileInfo[] files = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Libraries")).GetFiles("*.dll", SearchOption.AllDirectories);
                assemblies = (await Task.WhenAll(
                    files.Select(
                        async (file) =>
                        {
                            try
                            {
                                Assembly assembly = await AssemblyLoadContext.Default.LoadFromAssemblyPathAsync(file.FullName);
                                Logs.Info($"Loaded library \"{assembly.GetName().GetVersionedName()}\" from \"{file.FullName}\".");
                                return assembly;
                            }
                            catch (Exception ex)
                            {
                                Logs.Warning($"Error while loading library from \"{file.FullName}\": {ex}");
                                return null;
                            }
                        }
                    )
                )).Where(assembly => assembly != null)!;
                Logs.Info("Loaded libraries.");
            }
            catch (Exception ex)
            {
                Logs.Warning($"Error while loading libraries: {ex}");
            }
            try
            {
                Logs.Info("Initializing modules...");
                await Task.WhenAll(
                    assemblies.SelectMany(assembly =>
                        assembly.GetTypes().SelectMany(type =>
                            type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(method => (method.ReturnType == typeof(void) || method.ReturnType == typeof(Task)) && method.GetParameters().Length == 0).Select(
                                async (method) =>
                                {
                                    try
                                    {
                                        if (method.Invoke(null, null) is Task task) await task;
                                        Logs.Info($"Initialized module \"{type.Name}\" from \"{assembly.GetName().GetVersionedName()}\" using \"{method.Name}\".");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logs.Warning($"Error while initializing module \"{type.Name}\" from \"{assembly.GetName().GetVersionedName()}\" using \"{method.Name}\": {ex}");
                                    }
                                }
                            )
                        )
                    )
                );
                Logs.Info("Initialized modules.");
            }
            catch (Exception ex)
            {
                Logs.Warning($"Error while initializing modules: {ex}");
            }
            OnLoad(null, EventArgs.Empty);
            await Task.Delay(-1);
            //todo: shutdown handler and method
        }
    }
}