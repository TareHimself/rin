using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Windows;
using rin.Framework.Graphics.Windows.Events;
using rin.Framework.Widgets.Events;
using TerraFX.Interop.Vulkan;
using CharacterEvent = rin.Framework.Widgets.Events.CharacterEvent;
using CursorMoveEvent = rin.Framework.Widgets.Events.CursorMoveEvent;
using Events_CharacterEvent = rin.Framework.Widgets.Events.CharacterEvent;
using Events_CursorMoveEvent = rin.Framework.Widgets.Events.CursorMoveEvent;
using Events_ResizeEvent = rin.Framework.Widgets.Events.ResizeEvent;
using Events_ScrollEvent = rin.Framework.Widgets.Events.ScrollEvent;
using ResizeEvent = rin.Framework.Widgets.Events.ResizeEvent;
using ScrollEvent = rin.Framework.Widgets.Events.ScrollEvent;
using Widgets_Events_CharacterEvent = rin.Framework.Widgets.Events.CharacterEvent;
using Widgets_Events_CursorMoveEvent = rin.Framework.Widgets.Events.CursorMoveEvent;
using Widgets_Events_ResizeEvent = rin.Framework.Widgets.Events.ResizeEvent;
using Widgets_Events_ScrollEvent = rin.Framework.Widgets.Events.ScrollEvent;
using Windows_Events_CharacterEvent = rin.Framework.Graphics.Windows.Events.CharacterEvent;
using Windows_Events_CursorMoveEvent = rin.Framework.Graphics.Windows.Events.CursorMoveEvent;
using Windows_Events_ScrollEvent = rin.Framework.Graphics.Windows.Events.ScrollEvent;

namespace rin.Framework.Widgets.Graphics;

/// <summary>
///     A surface that displays widgets on a window
/// </summary>
public class WindowSurface : Surface
{
    private readonly WindowRenderer _renderer;
    public bool Minimized;
    public Vector2<int> Size;

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

    protected void OnRendererResized(Vector2<uint> size)
    {
        Size = size.Cast<int>();
        Minimized = Size.X == 0 || Size.Y == 0;
        if (!Minimized) ReceiveResize(new Widgets_Events_ResizeEvent(this, Size.Clone()));
    }
    
    protected void OnKeyboard(KeyEvent e)
    {
        ReceiveKeyboard(new KeyboardEvent(this,e.Key,e.State));
    }
    
    protected void OnCharacter(Windows_Events_CharacterEvent e)
    {
        ReceiveCharacter(new Widgets_Events_CharacterEvent(this,e.Data,e.Modifiers));
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
        ReceiveCursorMove(new Widgets_Events_CursorMoveEvent(this, e.Position.Cast<float>()));
    }

    protected void OnScroll(Windows_Events_ScrollEvent e)
    {
        ReceiveScroll(new Widgets_Events_ScrollEvent(this, e.Position.Cast<float>(), e.Delta.Cast<float>()));
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

    public override Vector2<float> GetCursorPosition()
    {
        return Window.GetCursorPosition().Cast<float>();
    }

    public override void SetCursorPosition(Vector2<float> position)
    {
        Window.SetMousePosition(position.Cast<double>());
    }

    public override Vector2<int> GetDrawSize()
    {
        return Size;
    }
}