using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Math;

namespace Rin.Engine.Views.Graphics;

public class ViewsFrame(
    Frame raw,
    IGraphImage drawImage,
    IGraphImage copyImage,
    IGraphImage stencilImage,
    SharedPassContext passContext)
{
    public readonly IGraphImage CopyImage = copyImage;
    public readonly IGraphImage DrawImage = drawImage;
    public readonly Frame Raw = raw;
    public readonly IGraphImage StencilImage = stencilImage;
    public Matrix4x4 ProjectionMatrix = passContext.ProjectionMatrix;
    public Extent2D Extent = passContext.Extent;
    // public SharedPassContext PassContext = passContext;
    
    // public void BeginMainPass(bool clearColor = false, bool clearStencil = false)
    // {
    //     //Surface.BeginMainPass(this, clearColor, clearStencil);
    // }
    //
    // public void EnsurePass(string passId, Action<ViewsFrame> applyPass)
    // {
    //     //Surface.EnsurePass(this, passId, applyPass);
    // }
    //
    // public void EndActivePass()
    // {
    //     //Surface.EndActivePass(this);
    // }
    

    public static implicit operator Frame(ViewsFrame frame)
    {
        return frame.Raw;
    }
}