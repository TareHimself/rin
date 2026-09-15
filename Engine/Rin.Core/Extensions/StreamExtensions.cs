using System.Buffers.Binary;
using Rin.Core.Shared;

namespace Rin.Core.Extensions;

public static class StreamExtensions
{
    extension(Stream stream)
    {
        public byte[] ReadAll()
        {
            var result = new byte[stream.Length];
            stream.ReadExactly(result, 0, (int)(stream.Length - stream.Position));
            return result;
        }

        public async Task<byte[]> ReadAllAsync()
        {
            var result = new byte[stream.Length];
            await stream.ReadExactlyAsync(result, 0, (int)(stream.Length - stream.Position));
            return result;
        }

        public void Write(in int data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, data);
            stream.Write(buffer);
        }

        public void Write(in float data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, data);
            stream.Write(buffer);
        }

        public void Write(in ulong data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, data);
            stream.Write(buffer);
        }

        public void Write(in double data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(double)];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, data);
            stream.Write(buffer);
        }

        public void Write(IBinarySerializable data)
        {
            data.BinarySerialize(stream);
        }

        public int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public ulong ReadUInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        public float ReadFloat()
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadSingleLittleEndian(buffer);
        }

        public double ReadDouble()
        {
            Span<byte> buffer = stackalloc byte[sizeof(double)];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        }

        public void Read(IBinarySerializable data)
        {
            data.BinaryDeserialize(stream);
        }
    }
}