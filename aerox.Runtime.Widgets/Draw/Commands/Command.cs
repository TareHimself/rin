using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets.Draw.Commands;

using static Vulkan;

public abstract class Command : Disposable
{
    public abstract void Bind(WidgetFrame frame);
    public abstract void Run(WidgetFrame frame);

    public void CmdDrawQuad(WidgetFrame frame)
    {
        vkCmdDraw(frame.Raw.GetCommandBuffer(), 6, 1, 0, 0);
    }


    protected override void OnDispose(bool isManual)
    {
    }
}