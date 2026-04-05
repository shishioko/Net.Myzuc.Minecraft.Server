namespace Net.Myzuc.Minecraft.Common.Packets
{
    public abstract class Packet
    {
        public enum ProtocolStageEnum
        {
            Handshake,
            Status,
            Login,
            Configuration,
            Play,
        }
        
        public abstract bool Serverbound { get; }
        public abstract ProtocolStageEnum ProtocolStage { get; }
        public abstract int Id { get; }
        
        public abstract void Deserialize(Stream stream);
        public abstract void Serialize(Stream stream);

        public static Packet? Create(bool serverbound, ProtocolStageEnum stage, int id)
        {
            return (serverbound, stage, id) switch
            {
                (HandshakePacket._Serverbound, HandshakePacket._ProtocolStage, HandshakePacket._Id) => new HandshakePacket(),
                _ => null
            };
        }
    }
}