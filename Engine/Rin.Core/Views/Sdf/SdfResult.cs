using Rin.Core.Graphics;

namespace Rin.Core.Views.Sdf;

public class SdfResult(IHostImage image, double width, double height) : IDisposable
{
    /// <summary>
    ///     Exact height of the generated sdf
    /// </summary>
    public readonly double Height = height;

    public readonly IHostImage Image = image;

    /// <summary>
    ///     Exact width of the generated sdf
    /// </summary>
    public readonly double Width = width;
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Image.Dispose();
    }
}