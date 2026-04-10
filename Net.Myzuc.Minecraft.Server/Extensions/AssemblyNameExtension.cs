using System.Reflection;

namespace Net.Myzuc.Minecraft.Server.Extensions
{
    internal static class AssemblyNameExtension
    {
        public static string GetVersionedName(this AssemblyName name)
        {
            return $"{name.Name}-{name.Version}";
        }
    }
}