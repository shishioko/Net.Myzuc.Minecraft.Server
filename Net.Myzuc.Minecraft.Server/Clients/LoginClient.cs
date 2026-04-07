using System.Collections.Concurrent;
using System.Net;
using Microsoft.VisualStudio.Threading;
using Net.Myzuc.Minecraft.Common.Protocol;
using Net.Myzuc.Minecraft.Common.Protocol.Packets;
using Net.Myzuc.Minecraft.Common.Protocol.Packets.Login;
using Net.Myzuc.Minecraft.Server.Objects.Events;

namespace Net.Myzuc.Minecraft.Server.Clients
{
    public sealed class LoginClient : Client
    {
        public event AsyncEventHandler<LoginStartEventArgs> OnStart = (sender, args) => Task.CompletedTask;
        public readonly bool IsTransfer;
        public required int ProtocolVersion { get; init; }
        public required (string address, ushort port) Origin { get; init; }
        
        private bool Ongoing = false;
        private bool Finishing = false;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]?>> OnCookie = [];
        private readonly ConcurrentDictionary<int, TaskCompletionSource<byte[]?>> OnCustom = [];
        private int CustomId = 0;
        private readonly TaskCompletionSource OnFinish = new();
        
        internal LoginClient(Connection connection, bool isTransfer) : base(connection, ProtocolStage.Login)
        {
            IsTransfer = isTransfer;
        }
        public async Task FinishAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            if (!Ongoing || Finishing) throw new ProtocolViolationException();
            Finishing = true;
            await WriteAsync(
                new LoginSuccessPacket()
                {
                    Profile = new(Guid.NewGuid(), "player")
                }
            );
            await OnFinish.Task.WaitAsync(cancellationToken.CombineWith(CancellationToken).Token);
        }
        public async Task SetCompressionThesholdAsync(int threshold)
        {
            await WriteAsync(
                new LoginCompressionPacket()
                {
                    Threshold = threshold
                }
            );
        }
        public async Task<byte[]?> GetCookieAsync(string identifier, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            if (!Ongoing || Finishing) throw new ProtocolViolationException();
            TaskCompletionSource<byte[]?> result = OnCookie.GetOrAdd(identifier, new TaskCompletionSource<byte[]?>());
            await WriteAsync(
                new LoginCookieRequestPacket()
                {
                    Id = identifier
                }
            );
            return await result.Task.WaitAsync(cancellationToken.CombineWith(CancellationToken).Token);
        }
        public async Task<byte[]?> SendCustomAsync(string channel, byte[] data, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            if (!Ongoing || Finishing) throw new ProtocolViolationException();
            int customId = CustomId;
            TaskCompletionSource<byte[]?> response = new();
            while (!OnCustom.TryAdd(customId, response)) customId = CustomId++;
            await WriteAsync(
                new LoginCustomRequestPacket()
                {
                    Id = customId,
                    Data = data,
                }
            );
            return await response.Task.WaitAsync(cancellationToken.CombineWith(CancellationToken).Token);
        }
        internal override async Task<Client?> HandlePacketAsync(Packet packet)
        {
            switch (packet)
            {
                case LoginStartPacket loginStartPacket:
                {
                    if (Ongoing || Finishing) throw new ProtocolViolationException();
                    Ongoing = true;
                    await OnStart.InvokeAsync(this, new(loginStartPacket.Name, loginStartPacket.Guid));
                    _ = Task.Run(
                        async () =>
                        {
                            await SetCompressionThesholdAsync(1);
                            byte[]? data = await GetCookieAsync("the_cookie");
                            await WriteAsync(
                                new LoginDisconnectPacket()
                                {
                                    Message = "goober"
                                }
                            );
                        });
                    break;
                }
                case LoginCookieResponsePacket loginCookieResponsePacket:
                {
                    if (!Ongoing) throw new ProtocolViolationException();
                    if (!OnCookie.TryGetValue(loginCookieResponsePacket.Id, out TaskCompletionSource<byte[]?>? result) && loginCookieResponsePacket.Id.StartsWith("minecraft:"))
                    {
                        OnCookie.TryGetValue(loginCookieResponsePacket.Id["minecraft:".Length..], out result);
                    }
                    if (result is null) throw new ProtocolViolationException();
                    result.TrySetResult(loginCookieResponsePacket.Data);
                    break;
                }
                case LoginCustomResponsePacket loginCustomResponsePacket:
                {
                    if (!Ongoing) throw new ProtocolViolationException();
                    if (!OnCustom.TryRemove(loginCustomResponsePacket.Id, out TaskCompletionSource<byte[]?>? response)) throw new ProtocolViolationException();
                    response.SetResult(loginCustomResponsePacket.Data);
                    break;
                }
                case LoginEndPacket:
                {
                    if (!Ongoing || !Finishing) throw new ProtocolViolationException();
                    Ongoing = false;
                    OnFinish.SetResult();
                    throw new NotImplementedException();
                    //break;
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