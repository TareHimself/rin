using rin.Framework.Core;

namespace rin.Framework.Views.Sdf;

public class Result(NativeBuffer<byte> data)
{
    public required int PixelWidth = 0;
    public required int PixelHeight = 0;
    public required double Width = 0;
    public required double Height = 0;
    public required int Channels = 0;
    public readonly NativeBuffer<byte> Data = data;
}