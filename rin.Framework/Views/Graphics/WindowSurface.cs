using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Windows;
using rin.Framework.Graphics.Windows.Events;
using rin.Framework.Views.Events;
using TerraFX.Interop.Vulkan;
using Views_Events_CharacterEvent = rin.Framework.Views.Events.CharacterEvent;
using Views_Events_CursorMoveEvent = rin.Framework.Views.Events.CursorMoveEvent;
using Views_Events_ResizeEvent = rin.Framework.Views.Events.ResizeEvent;
using Views_Events_ScrollEvent = rin.Framework.Views.Events.ScrollEvent;
using Windows_Events_CharacterEvent = rin.Framework.Graphics.Windows.Events.CharacterEvent;
using Windows_Events_CursorMoveEvent = rin.Framework.Graphics.Windows.Events.CursorMoveEvent;
using Windows_Events_ScrollEvent = rin.Framework.Graphics.Windows.Events.ScrollEvent;

namespace rin.Framework.Views.Graphics;

/// <summary>
///     A surface that displays views on a window
/// </summary>
public class WindowSurface : Surface
{
    private readonly WindowRenderer _renderer;
    public bool Minimized;
    public Vec2<int> Size;

    public WindowSurface(WindowRenderer renderer)
    {
        _renderer = renderer;
        Size = Window.GetPixelSize().Cast<int>();
    }

    public IWindow Window => _renderer.GetWindow();
    public WindowRenderer GetRenderer() => _renderer;

    public void CopyToSwapchain(Frame frame, VkImage swapchainImage, VkExtent2D extent)
    {
        // var drawImage = GetDrawImage();
        // drawImage.CopyTo(frame.GetCommandBuffer(),swapchainImage,_renderer.GetSwapchainExtent());
    }

    public override void Init()
    {
        base.Init();
        Window.OnCursorButton += OnMouseButton;
        Window.OnCursorMoved += OnMouseMove;
        Window.OnScrolled += OnScroll;
        Window.OnKey += OnKeyboard;
        Window.OnCharacter += OnCharacter;
        _renderer.OnDraw += Draw;
        _renderer.OnCopy += CopyToSwapchain;
        _renderer.OnResize += OnRendererResized;
    }

    protected void OnRendererResized(Vec2<uint> size)
    {
        Size = size.Cast<int>();
        Minimized = Size.X == 0 || Size.Y == 0;
        if (!Minimized) ReceiveResize(new Views_Events_ResizeEvent(this, Size.Clone()));
    }
    
    protected void OnKeyboard(KeyEvent e)
    {
        ReceiveKeyboard(new KeyboardEvent(this,e.Key,e.State));
    }
    
    protected void OnCharacter(Windows_Events_CharacterEvent e)
    {
        ReceiveCharacter(new Views_Events_CharacterEvent(this,e.Data,e.Modifiers));
    }

    protected void OnMouseButton(CursorButtonEvent e)
    {
        switch (e.State)
        {
            case InputState.Released:
                ReceiveCursorUp(new CursorUpEvent(this,e.Button,e.Position.Cast<float>()));
                break;
            case InputState.Pressed:
                ReceiveCursorDown(new CursorDownEvent(this,e.Button, e.Position.Cast<float>()));
                break;
            case InputState.Repeat:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected void OnMouseMove(Windows_Events_CursorMoveEvent e)
    {
        ReceiveCursorMove(new Views_Events_CursorMoveEvent(this, e.Position.Cast<float>()));
    }

    protected void OnScroll(Windows_Events_ScrollEvent e)
    {
        ReceiveScroll(new Views_Events_ScrollEvent(this, e.Position.Cast<float>(), e.Delta.Cast<float>()));
    }


    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _renderer.OnResize -= OnRendererResized;
        Window.OnCursorButton -= OnMouseButton;
        Window.OnCursorMoved -= OnMouseMove;
        Window.OnScrolled -= OnScroll;
        Window.OnKey -= OnKeyboard;
        Window.OnCharacter -= OnCharacter;
        _renderer.OnDraw -= Draw;
        _renderer.OnCopy -= CopyToSwapchain;
    }

    public override void Draw(Frame frame)
    {
        if (!Minimized) base.Draw(frame);
    }

    public override Vec2<float> GetCursorPosition()
    {
        return Window.GetCursorPosition().Cast<float>();
    }

    public override void SetCursorPosition(Vec2<float> position)
    {
        Window.SetMousePosition(position.Cast<double>());
    }

    public override Vec2<int> GetDrawSize()
    {
        return Size;
    }
}