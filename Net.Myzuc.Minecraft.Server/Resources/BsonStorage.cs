using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Net.Myzuc.Minecraft.Server.Resources
{
    public sealed class BsonStorage<T> : Storage<T> where T : class
    {
        private BsonBinaryReaderSettings BsonBinaryReaderSettings { get; }
        private BsonBinaryWriterSettings BsonBinaryWriterSettings { get; }
        public BsonStorage(string identifier, BsonBinaryReaderSettings? bsonBinaryReaderSettings = null, BsonBinaryWriterSettings? bsonBinaryWriterSettings = null) : base(identifier)
        {
            BsonBinaryReaderSettings = bsonBinaryReaderSettings ?? new()
            {
                Encoding = (UTF8Encoding)Encoding.UTF8,
                MaxDocumentSize = int.MaxValue,
                FixOldBinarySubTypeOnInput = false,
                FixOldDateTimeMaxValueOnInput = false
            };
            BsonBinaryWriterSettings = bsonBinaryWriterSettings ?? new()
            {
                Encoding = (UTF8Encoding)Encoding.UTF8,
                MaxDocumentSize = int.MaxValue,
                MaxSerializationDepth = 64,
                FixOldBinarySubTypeOnOutput =  false
            };
        }
        public override T Deserialize(byte[] data)
        {
            using MemoryStream ms = new(data);
            return BsonSerializer.Deserialize<T>(new BsonBinaryReader(ms, BsonBinaryReaderSettings));
        }
        public override byte[] Serialize(T data)
        {
            using MemoryStream ms = new();
            BsonSerializer.Serialize(new BsonBinaryWriter(ms, BsonBinaryWriterSettings), data);
            return ms.GetBuffer();
        }
        protected override string GetPath()
        {
            return $"{base.GetPath()}.bson";
        }
    }
}