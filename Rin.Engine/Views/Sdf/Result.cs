using Rin.Engine.Core;

namespace Rin.Engine.Views.Sdf;

public class Result(NativeBuffer<byte> data)
{
    public readonly NativeBuffer<byte> Data = data;
    public required int Channels = 0;
    public required double Height = 0;
    public required int PixelHeight = 0;
    public required int PixelWidth = 0;
    public required double Width = 0;
}