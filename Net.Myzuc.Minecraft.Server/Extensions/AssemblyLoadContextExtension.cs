using System.Reflection;
using System.Runtime.Loader;

namespace Net.Myzuc.Minecraft.Server.Extensions
{
    public static class AssemblyLoadContextExtensions
    {
        public static async Task<Assembly> LoadFromAssemblyPathAsync(this AssemblyLoadContext context, string assemblyPath, CancellationToken cancellationToken = default)
        {
            //if (!File.Exists(assemblyPath)) return null;
            byte[] binary = await File.ReadAllBytesAsync(assemblyPath, cancellationToken);
            using MemoryStream ms = new(binary);
            return context.LoadFromStream(ms);
        }
    }
}