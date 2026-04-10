using Net.Myzuc.Minecraft.Server.Clients;

namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public sealed class ConnectedEventArgs : EventArgs
    {
        public HandshakeClient Client { get; }
        internal ConnectedEventArgs(HandshakeClient client)
        {
            Client = client;
        }
    }
}