using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.FrameGraph;

namespace rin.Framework.Views.Graphics;

public struct SimpleRectPush
{
    public Mat3 Transform;

    public Vector2 Size;

    public Vector4 BorderRadius;

    public Vector4 Color;
}

public class ViewsFrame
{
    public readonly IGraphImage CopyImage;

    public readonly IGraphImage DrawImage;

    //public readonly AeroxLinkedList<GraphicsCommand> DrawCommandList = [];
    public readonly Frame Raw;
    public readonly IGraphImage StencilImage;
    public readonly Surface Surface;
    public readonly Vector2 SurfaceSize;
    public string ActivePass = "";
    public Mat4 Projection;
    public FrameStats Stats;

    public ViewsFrame(Surface surface, Frame raw, Vector2 surfaceSize, IGraphImage drawImage, IGraphImage copyImage,
        IGraphImage stencilImage, FrameStats stats)
    {
        Surface = surface;
        Raw = raw;
        Projection = Glm.Orthographic(0, surfaceSize.X, 0, surfaceSize.Y);
        DrawImage = drawImage;
        CopyImage = copyImage;
        StencilImage = stencilImage;
        SurfaceSize = surfaceSize;
        Stats = stats;
        //raw.OnReset += CleanupCommands;
    }

    public bool IsMainPassActive => ActivePass == Surface.MainPassId;
    public bool IsAnyPassActive => ActivePass.Length != 0;

    public void BeginMainPass(bool clearColor = false, bool clearStencil = false)
    {
        Surface.BeginMainPass(this, clearColor, clearStencil);
    }

    public void EnsurePass(string passId, Action<ViewsFrame> applyPass)
    {
        Surface.EnsurePass(this, passId, applyPass);
    }

    public void EndActivePass()
    {
        Surface.EndActivePass(this);
    }

    // public ViewFrame AddRect(Matrix3 transform, Vector2<float> size, Vector4<float>? borderRadius = null,
    //     Color? color = null)
    // {
    //     return AddCommands(new SimpleRect(transform, size)
    //     {
    //         BorderRadius = borderRadius,
    //         Color = color
    //     });
    // }
    //
    // public ViewFrame AddMaterialRect(MaterialInstance materialInstance, ViewPushConstants pushConstants)
    // {
    //     return AddCommands(new MaterialRect(materialInstance, pushConstants));
    // }
    //
    // public ViewFrame AddCommands(params GraphicsCommand[] commands)
    // {
    //     LinkedList<GraphicsCommand> lCommands = [];
    //     foreach (var command in commands)
    //     {
    //         DrawCommandList.InsertBack(command);
    //     }
    //     return this;
    // }
    //
    // public void CleanupCommands(Frame frame)
    // {
    //     foreach (var viewFrameDrawCommand in DrawCommandList) viewFrameDrawCommand.Value.Dispose();
    //     DrawCommandList.Clear();
    // }


    public static implicit operator Frame(ViewsFrame frame)
    {
        return frame.Raw;
    }
}