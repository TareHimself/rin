using rin.Core.Math;
using rin.Scene;
using rin.Runtime.Widgets;
using rin.Runtime.Widgets.Content;
using rin.Runtime.Widgets.Graphics.Commands;
using rin.Runtime.Widgets.Events;
using rin.Runtime.Widgets.Graphics;

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
        
        if(frame.IsMainPassActive) frame.Surface.EndActivePass(frame);
        
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
    public TextBox ModeText;
    private bool _ignoreNextMove = false;
    private Vector2<float> _mousePosition = 0.0f;
    private bool _captureMouse = false;

    public Vector2<float> AbsoluteCenter =>
        ((Vector2<float>)GetContentSize() / 2.0f).ApplyTransformation(ComputeAbsoluteTransform());
    public Viewport(Scene scene,TextBox modeText)
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

    protected override Vector2<float> ComputeDesiredContentSize() => new Vector2<float>();

    public override void Collect(WidgetFrame frame, TransformInfo info)
    {
        frame.AddCommands(new DrawSceneCommand(TargetScene,Channel));
    }

    public override void SetSize(Vector2<float> size)
    {
        base.SetSize(size);
        TargetScene.Drawer?.Resize(new Vector2<uint>((uint)Math.Ceiling(size.Width),(uint)Math.Ceiling(size.Height)));
    }

    public override void OnCursorUp(CursorUpEvent e)
    {
        if (_captureMouse)
        {
            _captureMouse = false;
            _ignoreNextMove = false;
        }
        base.OnCursorUp(e);
    }
    
    protected override bool OnCursorDown(CursorDownEvent e)
    {
        if (e.Button == MouseButton.One)
        {
            var currentIdx = (int)Channel;
            currentIdx = (currentIdx + 1) % 5;
            Channel = (EViewportChannel)currentIdx;
            UpdateModeText();
            return true;
        }

        if (e.Button == MouseButton.Two)
        {
            _captureMouse = true;
            _ignoreNextMove = true;
            _mousePosition = AbsoluteCenter;
            e.Surface.SetCursorPosition(_mousePosition);
            return true;
        }

        return false;
    }

    protected override bool OnCursorMove(CursorMoveEvent e)
    {
        if (_captureMouse)
        {
            var delta = e.Position - _mousePosition;

            if (!(Math.Abs(delta.X) > 0) && !(Math.Abs(delta.Y) > 0)) return true;
            
            OnMouseDelta(delta);
            _mousePosition = AbsoluteCenter;
            e.Surface.SetCursorPosition(_mousePosition);
            
            return true;
        }
        return base.OnCursorMove(e);
    }


    protected void OnMouseDelta(Vector2<float> delta)
    {
        Console.WriteLine($"Mouse Moved X : {delta.X} , Y : {delta.Y} ");
        var viewTarget = TargetScene.ViewTarget?.RootComponent;
        if(viewTarget == null) return;
        viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().ApplyYaw(delta.X).ApplyPitch(delta.Y));
    }
}