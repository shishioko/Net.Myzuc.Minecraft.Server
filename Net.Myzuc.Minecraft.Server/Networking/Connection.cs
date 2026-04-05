using System.IO.Compression;
using System.Net;
using Me.Shiokawaii.IO;
using Net.Myzuc.Minecraft.Common.Packets;

namespace Net.Myzuc.Minecraft.Server.Networking
{
    public class Connection : IDisposable, IAsyncDisposable
    {
        public static event EventHandler OnCreate = (sender, args) => { };
        
        public static void RegisterConnection(Connection connection)
        {
            try
            {
                Server.Logger.Debug($"Registering connection \"{connection.RemoteEndpoint}\"...");
                OnCreate(connection, EventArgs.Empty);
                //todo: handle
            }
            catch (Exception ex)
            {
                Server.Logger.Warn($"Error while registering connection \"{connection.RemoteEndpoint}\": {ex}");
            }
        }
        
        public bool Disposed { get; private set; }
        internal Stream Stream { get; set; }
        private EndPoint? RemoteEndpoint { get; set; }
        public bool KeepStreamOpen { init; private get; } = false;
        internal int CompressionThreshold { get; set; } = -1;
        public Packet.ProtocolStageEnum ProtocolStage { get; internal set; } = Packet.ProtocolStageEnum.Handshake;
        
        public event EventHandler<Packet> OnPacketRead = (sender, args) => {};
        public event EventHandler<Packet> OnPacketWrite = (sender, args) => {};
        public event EventHandler OnDispose = (sender, args) => { };
        
        public Connection(Stream stream, EndPoint? remoteEndpoint)
        {
            Stream = stream;
            RemoteEndpoint = remoteEndpoint;
        }
        public async Task<Packet> ReadAsync()
        {
            try
            {
                using MemoryStream ms = await readRawAsync();
                int id = ms.ReadS32V();
                Packet packet = Packet.Create(true, ProtocolStage, id) ?? throw new ProtocolViolationException($"Unknown Packet 0x{id:X2}!");
                packet.Deserialize(ms);
                OnPacketRead(this, packet);
                return packet;

                async Task<MemoryStream> readRawAsync()
                {
                    byte[] data = await Stream.ReadU8AAsync(await Stream.ReadS32VAsync());
                    if (CompressionThreshold < 0) return new(data);
                    MemoryStream ms2 = new(data);
                    int decompressedSize = ms2.ReadS32V();
                    if (decompressedSize <= 0) return ms2;
                    await using ZLibStream zlib = new(ms2, CompressionMode.Decompress, false);
                    return new(zlib.ReadU8A(decompressedSize));
                }
            }
            catch (Exception ex)
            {
                Server.Logger.Warn($"Error while reading from connection \"{RemoteEndpoint}\": {ex}");
                throw;
            }
        }
        public async Task WriteAsync(Packet packet)
        {
            try
            {
                OnPacketWrite(this, packet);
                using MemoryStream ms = new();
                ms.WriteS32V(packet.Id);
                packet.Serialize(ms);
                await writeRawAsync(ms.ToArray());
            }
            catch (Exception ex)
            {
                Server.Logger.Warn($"Error while reading writing to connection \"{RemoteEndpoint}\": {ex}");
                throw;
            }
            return;

            async Task writeRawAsync(byte[] data)
            {
                if (CompressionThreshold >= 0)
                {
                    using MemoryStream ms2 = new();
                    if (data.Length < CompressionThreshold)
                    {
                        ms2.WriteS32V(0);
                        ms2.WriteU8A(data);
                    }
                    else
                    {
                        using MemoryStream msCompressed = new();
                        await using ZLibStream zlib = new(msCompressed, CompressionMode.Compress, true);
                        zlib.WriteU8A(data);
                        byte[] compressed = msCompressed.ToArray();
                        ms2.WriteS32V(compressed.Length);
                        ms2.WriteU8A(compressed);
                    }
                    data = ms2.ToArray();
                }
                Stream.WriteS32V(data.Length);
                Stream.WriteU8A(data);
            }
        }
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            if (!KeepStreamOpen) Stream.Dispose();
            OnDispose(this, EventArgs.Empty);
        }
        public async ValueTask DisposeAsync()
        {
            if (Disposed) return;
            Disposed = true;
            if (!KeepStreamOpen) await Stream.DisposeAsync();
            OnDispose(this, EventArgs.Empty);
        }
    }
}