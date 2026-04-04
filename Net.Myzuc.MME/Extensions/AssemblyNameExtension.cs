using System.Reflection;

namespace Net.Myzuc.MME.Extensions
{
    public static class AssemblyNameExtension
    {
        public static string GetVersionedName(this AssemblyName name)
        {
            return $"{name.Name}-{name.Version}";
        }
    }
}