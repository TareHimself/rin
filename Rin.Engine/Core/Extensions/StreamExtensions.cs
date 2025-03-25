using System.Buffers.Binary;

namespace Rin.Engine.Core.Extensions;

public static class StreamExtensions
{
    public static byte[] ReadAll(this Stream stream)
    {
        var result = new byte[stream.Length];
        stream.ReadExactly(result, 0, (int)(stream.Length - stream.Position));
        return result;
    }

    public static async Task<byte[]> ReadAllAsync(this Stream stream)
    {
        var result = new byte[stream.Length];
        await stream.ReadExactlyAsync(result, 0, (int)(stream.Length - stream.Position));
        return result;
    }
    
    public static void Write(this Stream stream,in int data)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer,data);
        stream.Write(buffer);
    }
    
    public static void Write(this Stream stream,in float data)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteSingleLittleEndian(buffer,data);
        stream.Write(buffer);
    }
    
    public static void Write(this Stream stream,in UInt64 data)
    {
        Span<byte> buffer = stackalloc byte[sizeof(UInt64)];
        BinaryPrimitives.WriteSingleLittleEndian(buffer,data);
        stream.Write(buffer);
    }
    
    public static void Write(this Stream stream,in double data)
    {
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        BinaryPrimitives.WriteDoubleLittleEndian(buffer,data);
        stream.Write(buffer);
    }
    
    public static void Write(this Stream stream, IBinarySerializable data)
    {
        data.BinarySerialize(stream);
    }
    
    public static int ReadInt32(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }
    
    public static UInt64 ReadUInt64(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(UInt64)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
    }
    
    public static float ReadFloat(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadSingleLittleEndian(buffer);
    }
    
    public static double ReadDouble(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
    }
    
    public static void Read(this Stream stream, IBinarySerializable data)
    {
        data.BinaryDeserialize(stream);
    }
}