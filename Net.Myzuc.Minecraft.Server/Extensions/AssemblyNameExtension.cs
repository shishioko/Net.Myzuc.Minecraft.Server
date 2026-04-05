using System.Reflection;

namespace Net.Myzuc.Minecraft.Server.Extensions
{
    public static class AssemblyNameExtension
    {
        public static string GetVersionedName(this AssemblyName name)
        {
            return $"{name.Name}-{name.Version}";
        }
    }
}