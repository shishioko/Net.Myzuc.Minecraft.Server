using Net.Myzuc.Minecraft.Common.Data;

namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public sealed class LoginStartEventArgs : EventArgs
    {
        public ResolvedProfile Profile { get; }
        internal LoginStartEventArgs(ResolvedProfile profile)
        {
            Profile = profile;
        }
    }
}