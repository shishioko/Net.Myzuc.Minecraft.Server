using Net.Myzuc.Minecraft.Server.Clients;

namespace Net.Myzuc.Minecraft.Server.Objects.Events
{
    public sealed class ConnectedEventArgs : EventArgs
    {
        public readonly HandshakeClient Client;
        public ConnectedEventArgs(HandshakeClient client)
        {
            Client = client;
        }
    }
}