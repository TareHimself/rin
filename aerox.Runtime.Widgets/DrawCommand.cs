using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Widgets;

public abstract class DrawCommand : Disposable
{
    protected void Quad(WidgetFrame frame)
    {
        vkCmdDraw(frame.Raw.GetCommandBuffer(), 6, 1, 0, 0);
    }

    public abstract void Draw(WidgetFrame frame);

    protected override void OnDispose(bool isManual)
    {
    }
}