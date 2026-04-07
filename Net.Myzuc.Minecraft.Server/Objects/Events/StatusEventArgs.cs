using Net.Myzuc.Minecraft.Common.Data;

namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public class StatusEventArgs : EventArgs
    {
        public readonly Status Status;
        public StatusEventArgs(Status status)
        {
            Status = status;
        }
    }
}