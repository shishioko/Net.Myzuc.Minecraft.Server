using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using Microsoft.VisualStudio.Threading;
using Net.Myzuc.Minecraft.Common.Objects;
using Net.Myzuc.Minecraft.Common.Objects.ChatComponents;
using Net.Myzuc.Minecraft.Common.Primitives;
using Net.Myzuc.Minecraft.Common.Protocol;
using Net.Myzuc.Minecraft.Common.Protocol.Packets;
using Net.Myzuc.Minecraft.Common.Protocol.Packets.Login;
using Net.Myzuc.Minecraft.Common.Protocol.Packets.Login.Clientbound;
using Net.Myzuc.Minecraft.Common.Protocol.Packets.Login.Serverbound;
using Net.Myzuc.Minecraft.Common.Utilities;
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
        private readonly ConcurrentDictionary<Identifier, TaskCompletionSource<Memory<byte>?>> OnCookie = [];
        private readonly ConcurrentDictionary<int, TaskCompletionSource<Memory<byte>?>> OnCustom = [];
        private int CustomId = 0;
        private readonly TaskCompletionSource OnEncrypt = new();
        private ServersideEncryptionUtility? EncryptionUtility = null;
        private readonly TaskCompletionSource OnFinish = new();
        private ResolvedProfile Profile = new();
        
        internal LoginClient(Connection connection, bool isTransfer) : base(connection, ProtocolStage.Login)
        {
            IsTransfer = isTransfer;
        }
        public async Task FinishAsync(ResolvedProfile profile, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            if (!Ongoing || Finishing) throw new InvalidOperationException();
            Finishing = true;
            await WriteAsync(new SuccessPacket()
            {
                Profile = profile
            });
            await OnFinish.Task.WaitAsync(cancellationToken.CombineWith(CancellationToken).Token);
        }
        public async Task SetCompressionThesholdAsync(int threshold)
        {
            if (!Ongoing || Finishing) throw new InvalidOperationException();
            await WriteAsync(
                new CompressionPacket()
                {
                    Threshold = threshold
                }
            );
        }
        public async Task<ResolvedProfile> EncryptAsync(bool authenticate, string serverId = "", CancellationToken cancellationToken = default)
        {
            if (!Ongoing || Finishing) throw new InvalidOperationException();
            if (EncryptionUtility is not null) throw new InvalidOperationException();
            EncryptionUtility = new(authenticate, Profile.Name,  serverId);
            await WriteAsync(EncryptionUtility.GenerateRequest());
            await OnEncrypt.Task.WaitAsync(cancellationToken.CombineWith(CancellationToken).Token);
            return Profile;
        }
        public async Task<Memory<byte>?> GetCookieAsync(Identifier identifier, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            if (!Ongoing || Finishing) throw new InvalidOperationException();
            TaskCompletionSource<Memory<byte>?> result = OnCookie.GetOrAdd(identifier, new TaskCompletionSource<Memory<byte>?>());
            await WriteAsync(
                new CookieRequestPacket()
                {
                    Id = identifier
                }
            );
            return await result.Task.WaitAsync(cancellationToken.CombineWith(CancellationToken).Token);
        }
        public async Task<Memory<byte>?> SendCustomAsync(string channel, Memory<byte> data, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            if (!Ongoing || Finishing) throw new InvalidOperationException();
            int customId = CustomId;
            TaskCompletionSource<Memory<byte>?> response = new();
            while (!OnCustom.TryAdd(customId, response)) customId = CustomId++;
            await WriteAsync(
                new CustomRequestPacket()
                {
                    Id = customId,
                    Data = data,
                }
            );
            return await response.Task.WaitAsync(cancellationToken.CombineWith(CancellationToken).Token);
        }
        public async Task DisconnectAsync(ChatComponent message)
        {
            await WriteAsync(new DisconnectPacket()
            {
                Message = message
            });
        }
        internal override async Task<Client?> HandlePacketAsync(IPacket packet)
        {
            switch (packet)
            {
                case StartPacket loginStartPacket:
                {
                    if (Ongoing || Finishing) throw new ProtocolViolationException();
                    Ongoing = true;
                    Profile = new(loginStartPacket.Guid, loginStartPacket.Name);
                    await OnStart.InvokeAsync(this, new(Profile));
                    break;
                }
                case EncryptionResponsePacket encryptionResponsePacket:
                {
                    if (!Ongoing) throw new ProtocolViolationException();
                    if (EncryptionUtility is null) throw new ProtocolViolationException();
                    try
                    {
                        byte[]? secret = EncryptionUtility.HandleResponse(encryptionResponsePacket);
                        if (secret is null) throw new CryptographicException();
                        Connection.Encrypt(secret);
                        if (EncryptionUtility.Authenticate) Profile = await EncryptionUtility.AuthenticateAsync();
                        OnEncrypt.SetResult();
                    }
                    catch (Exception ex)
                    {
                        OnEncrypt.SetException(ex);
                        throw;
                    }
                    break;
                }
                case CookieResponsePacket loginCookieResponsePacket:
                {
                    if (!Ongoing) throw new ProtocolViolationException();
                    OnCookie.TryRemove(loginCookieResponsePacket.Id, out TaskCompletionSource<Memory<byte>?>? result);
                    if (result is null) throw new ProtocolViolationException();
                    result.TrySetResult(loginCookieResponsePacket.Data);
                    break;
                }
                case CustomResponsePacket loginCustomResponsePacket:
                {
                    if (!Ongoing) throw new ProtocolViolationException();
                    if (!OnCustom.TryRemove(loginCustomResponsePacket.Id, out TaskCompletionSource<Memory<byte>?>? response)) throw new ProtocolViolationException();
                    response.SetResult(loginCustomResponsePacket.Data);
                    break;
                }
                case EndPacket:
                {
                    if (!Ongoing || !Finishing) throw new ProtocolViolationException();
                    Ongoing = false;
                    OnFinish.SetResult();
                    return new ConfigurationClient(Connection);
                    break;
                }
                default:
                {
                    throw new ProtocolViolationException("Unexpected Packet!");
                }
            }
            return null;
        }
        public override void Dispose()
        {
            EncryptionUtility?.Dispose();
            base.Dispose();
        }
        public override ValueTask DisposeAsync()
        {
            EncryptionUtility?.Dispose();
            return base.DisposeAsync();
        }
    }
}