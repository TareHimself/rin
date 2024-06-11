using aerox.Runtime.Math;
using aerox.Runtime.Scene;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Draw.Commands;
using TerraFX.Interop.Vulkan;

namespace SceneTest;


internal class DrawSceneCommand(Scene scene) : UtilityCommand
{
    public override void Run(WidgetFrame frame)
    {
        var drawer = scene.Drawer;
        var renderTarget = drawer?.RenderTarget;
        if(drawer == null || renderTarget == null) return;
        
        if(frame.IsMainPassActive) frame.Surface.EndMainPass(frame);
        
        drawer.Draw(frame);
        // var cmd = frame.Raw.GetCommandBuffer();
        // renderTarget.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
        // frame.Surface.GetDrawImage().Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL);
        // renderTarget.CopyTo(cmd,frame.Surface.GetDrawImage());
        // frame.Surface.GetDrawImage().Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
    }
}

internal class DrawViewportCommand(Scene scene) : DrawCommand
{
    protected override void Draw(WidgetFrame frame)
    {
        
    }
}
public class Viewport : Widget
{
    public readonly Scene TargetScene;

    public Viewport(Scene scene)
    {
        TargetScene = scene;
    }
    
    public override Size2d ComputeDesiredSize() => new Size2d();

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        frame.AddCommands(new DrawSceneCommand(TargetScene));
    }

    public override void SetDrawSize(Size2d size)
    {
        base.SetDrawSize(size);
        TargetScene.Drawer?.OnResize(new Vector2<int>((int)Math.Ceiling(size.Width),(int)Math.Ceiling(size.Height)));
    }
}