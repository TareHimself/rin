using aerox.Runtime.Math;
using aerox.Runtime.Scene;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Defaults.Content;
using aerox.Runtime.Widgets.Draw.Commands;
using aerox.Runtime.Widgets.Events;
using TerraFX.Interop.Vulkan;

namespace SceneTest;

public enum EViewportChannel
{
    Scene,
    Color,
    Location,
    Normal,
    RoughnessMetallicSpecular,
    Emissive
}
internal class DrawSceneCommand(Scene scene,EViewportChannel channel) : UtilityCommand
{
    public override void Run(WidgetFrame frame)
    {
        var drawer = scene.Drawer;
        var source = channel switch
        {
            EViewportChannel.Scene => drawer?.RenderTarget,
            EViewportChannel.Color => drawer?.Images?.Color,
            EViewportChannel.Location => drawer?.Images?.Location,
            EViewportChannel.Normal => drawer?.Images?.Normal,
            EViewportChannel.RoughnessMetallicSpecular => drawer?.Images?.RoughnessMetallicSpecular,
            EViewportChannel.Emissive => drawer?.Images?.Emissive,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
        };
        
        if(drawer == null || source == null) return;
        
        if(frame.IsMainPassActive) frame.Surface.EndMainPass(frame);
        
        drawer.Draw(frame);
        var cmd = frame.Raw.GetCommandBuffer();
        
        source.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);
        frame.Surface.GetDrawImage().Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL);
        source.CopyTo(cmd,frame.Surface.GetDrawImage());
        frame.Surface.GetDrawImage().Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
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
    public EViewportChannel Channel = EViewportChannel.Scene;
    public Text ModeText;

    public Viewport(Scene scene,Text modeText)
    {
        TargetScene = scene;
        ModeText = modeText;
        UpdateModeText();
    }

    public void UpdateModeText()
    {
        ModeText.Content = Channel switch
        {
            EViewportChannel.Scene => "Default",
            EViewportChannel.Color => "Color",
            EViewportChannel.Location => "Location",
            EViewportChannel.Normal => "Normal",
            EViewportChannel.RoughnessMetallicSpecular => "Roughness Metallic Specular",
            EViewportChannel.Emissive => "Emissive",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public override Size2d ComputeDesiredSize() => new Size2d();

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        frame.AddCommands(new DrawSceneCommand(TargetScene,Channel));
    }

    public override void SetDrawSize(Size2d size)
    {
        base.SetDrawSize(size);
        TargetScene.Drawer?.Resize(new Vector2<int>((int)Math.Ceiling(size.Width),(int)Math.Ceiling(size.Height)));
    }

    protected override bool OnCursorDown(CursorDownEvent e)
    {
        var currentIdx = (int)Channel;
        currentIdx = (currentIdx + 1) % 5;
        Channel = (EViewportChannel)currentIdx;
        UpdateModeText();
        return true;
    }
}