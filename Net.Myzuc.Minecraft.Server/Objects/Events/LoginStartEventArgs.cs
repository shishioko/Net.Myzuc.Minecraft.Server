using Net.Myzuc.Minecraft.Common.Objects;

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