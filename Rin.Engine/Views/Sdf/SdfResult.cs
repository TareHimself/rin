using Rin.Engine.Core;
using Rin.Engine.Core.Extensions;

namespace Rin.Engine.Views.Sdf;

public class SdfResult : IBinarySerializable
{
    public readonly NativeBuffer<byte> Data;
    public int Channels = 0;
    public double Width = 0;
    public double Height = 0;
    public int PixelWidth = 0;
    public int PixelHeight = 0;
    
    public SdfResult(NativeBuffer<byte> data, int channels, double width,double height, int pixelWidth,int pixelHeight)
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
        Data = new NativeBuffer<byte>();
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
}