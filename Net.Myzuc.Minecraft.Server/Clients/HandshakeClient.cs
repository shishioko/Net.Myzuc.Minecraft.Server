using System.Net;
using Net.Myzuc.Minecraft.Common.Data.Enums;
using Net.Myzuc.Minecraft.Common.Protocol;
using Net.Myzuc.Minecraft.Common.Protocol.Packets;
using Net.Myzuc.Minecraft.Common.Protocol.Packets.Handshake;

namespace Net.Myzuc.Minecraft.Server.Clients
{
    public sealed class HandshakeClient : Client
    {
        internal HandshakeClient(Connection connection) : base(connection, ProtocolStage.Handshake)
        {
            
        }
        internal override async Task<Client?> HandlePacketAsync(Packet packet)
        {
            switch (packet)
            {
                case HandshakePacket handshakePacket:
                {
                    switch (handshakePacket.Intent)
                    {
                        case HandshakeIntent.Status:
                        {
                            return new StatusClient(Connection)
                            {
                                ProtocolVersion = handshakePacket.ProtocolVersion,
                                Origin = (handshakePacket.Address, handshakePacket.Port),
                            };
                        }
                        case HandshakeIntent.Login:
                        case HandshakeIntent.Transfer:
                        {
                            return new LoginClient(Connection, handshakePacket.Intent == HandshakeIntent.Transfer)
                            {
                                ProtocolVersion = handshakePacket.ProtocolVersion,
                                Origin = (handshakePacket.Address, handshakePacket.Port),
                            };
                        }
                        default:
                        {
                            throw new ProtocolViolationException("Unexpected Intent!");
                        }
                    }
                    //break;
                }
                default:
                {
                    throw new ProtocolViolationException("Unexpected Packet!");
                }
            }
            //return null;
        }
    }
}