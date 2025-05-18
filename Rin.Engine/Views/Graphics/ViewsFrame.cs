using System.Numerics;
using Rin.Engine.Graphics;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Views.Graphics;

public class ViewsFrame(
    SharedPassContext passContext,
    in VkCommandBuffer cmd)
{
    public VkCommandBuffer CommandBuffer = cmd;
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