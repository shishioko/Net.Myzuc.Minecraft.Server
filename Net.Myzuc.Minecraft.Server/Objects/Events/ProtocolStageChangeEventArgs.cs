using Net.Myzuc.Minecraft.Server.Clients;

namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public sealed class ProtocolStageChangeEventArgs : EventArgs
    {
        public readonly Client? Client;
        public ProtocolStageChangeEventArgs(Client? client)
        {
            Client = client;
        }
    }
}