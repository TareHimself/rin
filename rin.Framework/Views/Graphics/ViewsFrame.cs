using System.Runtime.InteropServices;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;

namespace rin.Framework.Views.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct SimpleRectPush
{
    public Matrix3 Transform;

    public Vector2<float> Size;

    public Vector4<float> BorderRadius;

    public Vector4<float> Color;
}

public class ViewsFrame
{
    //public readonly AeroxLinkedList<GraphicsCommand> DrawCommandList = [];
    public readonly Frame Raw;
    public readonly Surface Surface;
    public string ActivePass = "";
    public bool IsMainPassActive => ActivePass == Surface.MainPassId;
    public bool IsAnyPassActive => ActivePass.Length != 0;
    public Matrix4 Projection;

    public readonly IDeviceImage DrawImage;
    public readonly IDeviceImage CopyImage;
    public readonly IDeviceImage StencilImage;

    public ViewsFrame(Surface surface, Frame raw,IDeviceImage drawImage,IDeviceImage copyImage,IDeviceImage stencilImage)
    {
        Surface = surface;
        Raw = raw;
        var size = surface.GetDrawSize();
        Projection = Glm.Orthographic(0, size.X, 0, size.Y);
        DrawImage = drawImage;
        CopyImage = copyImage;
        StencilImage = stencilImage;
        //raw.OnReset += CleanupCommands;
    }

    public void BeginMainPass(bool clearColor = false,bool clearStencil = false)
    {
        Surface.BeginMainPass(this,clearColor,clearStencil);
    }

    public void EnsurePass(string passId,Action<ViewsFrame> applyPass) => Surface.EnsurePass(this, passId,applyPass);

    public void EndActivePass()
    {
        Surface.EndActivePass(this);
    }

    // public WidgetFrame AddRect(Matrix3 transform, Vector2<float> size, Vector4<float>? borderRadius = null,
    //     Color? color = null)
    // {
    //     return AddCommands(new SimpleRect(transform, size)
    //     {
    //         BorderRadius = borderRadius,
    //         Color = color
    //     });
    // }
    //
    // public WidgetFrame AddMaterialRect(MaterialInstance materialInstance, WidgetPushConstants pushConstants)
    // {
    //     return AddCommands(new MaterialRect(materialInstance, pushConstants));
    // }
    //
    // public WidgetFrame AddCommands(params GraphicsCommand[] commands)
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
    //     foreach (var widgetFrameDrawCommand in DrawCommandList) widgetFrameDrawCommand.Value.Dispose();
    //     DrawCommandList.Clear();
    // }


    public static implicit operator Frame(ViewsFrame frame)
    {
        return frame.Raw;
    }
}