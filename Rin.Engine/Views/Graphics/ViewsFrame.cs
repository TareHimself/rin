using System.Numerics;
using Rin.Engine.Graphics;

namespace Rin.Engine.Views.Graphics;

public class ViewsFrame(
    SharedPassContext passContext,
    IExecutionContext executionContext)
{
    public IExecutionContext ExecutionContext = executionContext;
    public Extent2D Extent = passContext.Extent;

    public Matrix4x4 ProjectionMatrix = passContext.ProjectionMatrix;
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