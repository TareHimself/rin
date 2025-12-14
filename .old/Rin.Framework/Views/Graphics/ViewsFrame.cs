using System.Numerics;
using Rin.Framework.Graphics;

namespace Rin.Framework.Views.Graphics;

public class ViewsFrame(
    SurfaceContext context,
    IExecutionContext executionContext)
{
    public IExecutionContext ExecutionContext = executionContext;
    public Extent2D Extent = context.Extent;

    public Matrix4x4 ProjectionMatrix = context.ProjectionMatrix;
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
}