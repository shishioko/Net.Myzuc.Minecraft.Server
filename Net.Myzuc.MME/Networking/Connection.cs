using System.IO.Compression;
using System.Net;
using Me.Shiokawaii.IO;
using Net.Myzuc.MME.Data.Packets;
using Net.Myzuc.MME.Events;

namespace Net.Myzuc.MME.Networking
{
    public class Connection : IDisposable, IAsyncDisposable
    {
        public static event EventHandler OnCreate = (sender, args) => { };
        
        public static void RegisterConnection(Connection connection)
        {
            OnCreate(connection, EventArgs.Empty);
            //todo: handle
        }
        
        public bool Disposed { get; private set; }
        internal Stream Stream { get; set; }
        public bool KeepStreamOpen { init; private get; } = false;
        internal int CompressionThreshold { get; set; } = -1;
        public Packet.ProtocolStageEnum ProtocolStage { get; internal set; } = Packet.ProtocolStageEnum.Handshake;
        
        public event EventHandler<PacketEventArgs> OnPacketRead = (sender, args) => {};
        public event EventHandler<PacketEventArgs> OnPacketWrite = (sender, args) => {};
        public event EventHandler OnDispose = (sender, args) => { };
        
        public Connection(Stream stream)
        {
            Stream = stream;
        }
        public async Task<Packet> ReadAsync()
        {
            using MemoryStream ms = await readRawAsync();
            int id = ms.ReadS32V();
            Packet packet = Packet.Create(true, ProtocolStage, id) ?? throw new ProtocolViolationException($"Unknown Packet 0x{id:X2}!");
            packet.Deserialize(ms);
            PacketEventArgs args = new()
            {
                Packet = packet,
            };
            OnPacketRead(this, args);
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
        public async Task WriteAsync(Packet packet)
        {
            PacketEventArgs args = new()
            {
                Packet = packet,
            };
            OnPacketWrite(this, args);
            using MemoryStream ms = new();
            ms.WriteS32V(packet.Id);
            packet.Serialize(ms);
            await writeRawAsync(ms.ToArray());
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