using Net.Myzuc.Minecraft.Server.Clients;

namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public sealed class ProtocolStageChangeEventArgs : EventArgs
    {
        public Client? Client { get; }
        internal ProtocolStageChangeEventArgs(Client? client)
        {
            Client = client;
        }
    }
}