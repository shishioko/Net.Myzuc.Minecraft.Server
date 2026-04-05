using Me.Shiokawaii.IO;

namespace Net.Myzuc.Minecraft.Server.Resources
{
    public sealed class BinaryStorage<T> : Storage<T> where T : class, new()
    {
        public BinaryStorage(string identifier) : base(identifier)
        {
            
        }
        public override T Deserialize(byte[] data)
        {
            using MemoryStream ms = new(data);
            using SerialStream stream = new(ms)
            {
                AutoClose = true,
                DynamicPrefix = false,
                LittleEndian = false,
                LongPrefix = false,
            };
            return stream.Read<T>();
        }
        public override byte[] Serialize(T data)
        {
            using MemoryStream ms = new();
            using SerialStream stream = new(ms)
            {
                AutoClose = true,
                DynamicPrefix = false,
                LittleEndian = false,
                LongPrefix = false,
            };
            stream.Write(data);
            return ms.ToArray();
        }
        protected override string GetPath()
        {
            return $"{base.GetPath()}.bin";
        }
    }
}