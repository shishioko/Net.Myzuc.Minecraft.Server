using Net.Myzuc.Minecraft.Common.Data;

namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public sealed class LoginStartEventArgs : EventArgs
    {
        public readonly ResolvedProfile Profile = new();
        public LoginStartEventArgs(ResolvedProfile profile)
        {
            Profile = profile;
        }
    }
}