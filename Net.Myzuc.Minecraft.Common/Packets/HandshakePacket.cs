using System.Text;
using Me.Shiokawaii.IO;
using Net.Myzuc.Minecraft.Server.Extensions;

namespace Net.Myzuc.Minecraft.Common.Packets
{
    public sealed class HandshakePacket : Packet
    {
        public enum IntentEnum : int
        {
            Status = 1,
            Login = 2,
            Transfer = 3,
        }

        internal const bool _Serverbound = true;
        internal const ProtocolStageEnum _ProtocolStage = ProtocolStageEnum.Handshake;
        internal const int _Id = 0x00;

        public override bool Serverbound => _Serverbound;
        public override ProtocolStageEnum ProtocolStage => _ProtocolStage;
        public override int Id => _Id;
        
        public int ProtocolVersion = 0;
        public string Address = "";
        public ushort Port = 0;
        public IntentEnum Intent = IntentEnum.Status;
        
        public override void Serialize(Stream stream)
        {
            stream.WriteS32V(ProtocolVersion);
            stream.WriteMinecraftString(Address);
            stream.WriteU16(Port);
            stream.WriteS32V((int)Intent);
        }
        public override void Deserialize(Stream stream)
        {
            ProtocolVersion = stream.ReadS32V();
            Address = stream.ReadMinecraftString();
            Port = stream.ReadU16();
            Intent = (IntentEnum)stream.ReadS32V();
        }
    }
}