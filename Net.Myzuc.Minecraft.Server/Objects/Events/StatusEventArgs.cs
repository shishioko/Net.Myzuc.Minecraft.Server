using Net.Myzuc.Minecraft.Common.Data;

namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public sealed class StatusEventArgs : EventArgs
    {
        public Status Status { get; }
        internal StatusEventArgs(Status status)
        {
            Status = status;
        }
    }
}