using Rin.Engine.Core;
using Rin.Engine.Core.Extensions;

namespace Rin.Engine.Views.Sdf;

public class SdfResult : IBinarySerializable, IDisposable
{
    public readonly Buffer<byte> Data;
    public int Channels;
    public double Height;
    public int PixelHeight;
    public int PixelWidth;
    public double Width;

    public SdfResult(Buffer<byte> data, int channels, double width, double height, int pixelWidth, int pixelHeight)
    {
        Data = data;
        Channels = channels;
        Width = width;
        Height = height;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    public SdfResult()
    {
        Data = new Buffer<byte>();
    }

    public void BinarySerialize(Stream output)
    {
        output.Write(Channels);
        output.Write(Width);
        output.Write(Height);
        output.Write(PixelWidth);
        output.Write(PixelHeight);
        output.Write((IBinarySerializable)Data);
    }

    public void BinaryDeserialize(Stream input)
    {
        Channels = input.ReadInt32();
        Width = input.ReadDouble();
        Height = input.ReadDouble();
        PixelWidth = input.ReadInt32();
        PixelHeight = input.ReadInt32();
        input.Read((IBinarySerializable)Data);
    }

    public void Dispose()
    {
        Data.Dispose();
    }
}