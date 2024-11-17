using rin.Graphics;
using rin.Core.Math;
using rin.Graphics.Windows;
using rin.Graphics.Windows.Events;
using rin.Widgets.Events;
using rin.Windows;
using TerraFX.Interop.Vulkan;
using CharacterEvent = rin.Widgets.Events.CharacterEvent;
using CursorMoveEvent = rin.Widgets.Events.CursorMoveEvent;
using ResizeEvent = rin.Widgets.Events.ResizeEvent;
using ScrollEvent = rin.Widgets.Events.ScrollEvent;

namespace rin.Widgets.Graphics;

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
        Size = new Vector2<int>((int)Window.PixelSize.X, (int)Window.PixelSize.Y);
    }

    public Window Window => _renderer.GetWindow();
    public WindowRenderer GetRenderer() => _renderer;

    public void CopyToSwapchain(Frame frame, VkImage swapchainImage, VkExtent2D extent)
    {
        var drawImage = GetDrawImage();
        drawImage.CopyTo(frame.GetCommandBuffer(),swapchainImage,_renderer.GetSwapchainExtent());
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
        _renderer.OnCopyToSwapchain += CopyToSwapchain;
        _renderer.OnResize += OnRendererResized;
    }

    protected void OnRendererResized(Vector2<uint> size)
    {
        Size = size.Cast<int>();
        Minimized = Size.X == 0 || Size.Y == 0;
        if (!Minimized) ReceiveResize(new ResizeEvent(this, Size.Clone()));
    }
    
    protected void OnKeyboard(KeyEvent e)
    {
        ReceiveKeyboard(new KeyboardEvent(this,e.Key,e.State));
    }
    
    protected void OnCharacter(rin.Graphics.Windows.Events.CharacterEvent e)
    {
        ReceiveCharacter(new CharacterEvent(this,e.Data,e.Modifiers));
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

    protected void OnMouseMove(rin.Graphics.Windows.Events.CursorMoveEvent e)
    {
        ReceiveCursorMove(new CursorMoveEvent(this, e.Position.Cast<float>()));
    }

    protected void OnScroll(rin.Graphics.Windows.Events.ScrollEvent e)
    {
        ReceiveScroll(new ScrollEvent(this, e.Position.Cast<float>(), e.Delta.Cast<float>()));
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
        _renderer.OnCopyToSwapchain -= CopyToSwapchain;
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