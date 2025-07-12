using Rin.Engine.Extensions;
using Rin.Engine.Graphics;

namespace Rin.Engine.Views.Sdf;

public class SdfResult(IHostImage image, double width, double height) : IDisposable
{
    public readonly IHostImage Image = image;
    public readonly double Height = height;
    public readonly double Width = width;

    // public void BinarySerialize(Stream output)
    // {
    //     output.Write(Channels);
    //     output.Write(Width);
    //     output.Write(Height);
    //     output.Write(PixelWidth);
    //     output.Write(PixelHeight);
    //     output.Write((IBinarySerializable)Data);
    // }
    //
    // public void BinaryDeserialize(Stream input)
    // {
    //     Channels = input.ReadInt32();
    //     Width = input.ReadDouble();
    //     Height = input.ReadDouble();
    //     PixelWidth = input.ReadInt32();
    //     PixelHeight = input.ReadInt32();
    //     input.Read((IBinarySerializable)Data);
    // }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Image?.Dispose();
    }
}