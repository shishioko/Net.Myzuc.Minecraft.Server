using Net.Myzuc.MME.Data.Packets;

namespace Net.Myzuc.MME.Events
{
    public class PacketEventArgs : EventArgs
    {
        public required Packet Packet { get; init; }
    }
}