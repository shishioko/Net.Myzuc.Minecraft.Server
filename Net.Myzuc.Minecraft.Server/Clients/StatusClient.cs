using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.Threading;
using Net.Myzuc.Minecraft.Common.Protocol;
using Net.Myzuc.Minecraft.Common.Protocol.Packets;
using Net.Myzuc.Minecraft.Common.Protocol.Packets.Status;
using Net.Myzuc.Minecraft.Server.Objects.Events;
using Net.Myzuc.Minecraft.Server.Extensions;

namespace Net.Myzuc.Minecraft.Server.Clients
{
    public sealed class StatusClient : Client
    {
        public event AsyncEventHandler<StatusEventArgs> OnStatus = async (sender, args) => {};
        public required int ProtocolVersion { get; init; }
        public required (string address, ushort port) Origin { get; init; }
        internal StatusClient(Connection connection) : base(connection, ProtocolStage.Status)
        {
            
        }
        internal override async Task<Client?> HandlePacketAsync(IPacket packet)
        {
            switch (packet)
            {
                case StatusRequestPacket statusRequestPacket:
                {
                    StatusEventArgs args = new(new()
                    {
                        Version = new($"{Assembly.GetExecutingAssembly().GetName().GetVersionedName()} for {Common.Minecraft.Version}", Common.Minecraft.ProtocolVersion),
                        Players = null,
                        Description = $"A {Assembly.GetExecutingAssembly().GetName().Name} Server",
                        EnforcesSecureChat = false,
                    });
                    await OnStatus.InvokeAsync(this, args);
                    await WriteAsync(
                        new StatusResponsePacket()
                        {
                            Status = args.Status
                        }
                    );
                    break;
                }
                case PingRequestPacket pingRequestPacket:
                {
                    await WriteAsync(
                        new PingResponsePacket()
                        {
                            Data = pingRequestPacket.Data
                        }
                    );
                    await DisposeAsync();
                    break;
                }
                default:
                {
                    throw new ProtocolViolationException("Unexpected Packet!");
                }
            }
            return null;
        }
    }
}