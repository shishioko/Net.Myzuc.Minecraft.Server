using Microsoft.VisualStudio.Threading;
using Net.Myzuc.Minecraft.Common.Protocol;
using Net.Myzuc.Minecraft.Common.Protocol.Packets;
using Net.Myzuc.Minecraft.Server.Objects.Events;

namespace Net.Myzuc.Minecraft.Server.Clients
{
    public abstract class Client : IDisposable, System.IAsyncDisposable
    {
        public event AsyncEventHandler<ProtocolStageChangeEventArgs> OnProtocolStageChange = (sender, args) => Task.CompletedTask;
        
        protected readonly Connection Connection;
        
        protected CancellationToken CancellationToken => CancellationTokenSource.Token;
        private readonly CancellationTokenSource CancellationTokenSource = new();
        
        protected bool Disposed { get; private set; } = false;
        private bool Listening = false;
        internal Client(Connection connection, ProtocolStage protocolStage)
        {
            if (connection.ProtocolStage != protocolStage) throw new InvalidOperationException("Protocol stage mismatches!");
            Connection = connection;
        }
        protected async Task<Packet> ReadAsync()
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            return await Connection.ReadAsync();
        }
        protected async Task WriteAsync(Packet packet)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            await Connection.WriteAsync(packet);
        }
        internal async Task<Client?> ListenAsync()
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            if (Listening) throw new InvalidOperationException();
            Listening = true;
            try
            {
                while (!Disposed)
                {
                    Packet packet = await ReadAsync();
                    Client? newClient = await HandlePacketAsync(packet);
                    if (Connection.ProtocolStage != ProtocolStage.Disconnected && newClient == null) continue;
                    await OnProtocolStageChange.InvokeAsync(this, new(newClient));
                    return newClient;
                }
            }
            catch (Exception ex)
            {
                Server.Logger.Trace($"Error while listening to client: {ex}");
            }
            finally
            {
                await DisposeAsync();
            }
            return null;
        }
        internal abstract Task<Client?> HandlePacketAsync(Packet packet);
        public virtual void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
        }
        public virtual async ValueTask DisposeAsync()
        {
            if (Disposed) return;
            Disposed = true;
        }
    }
}