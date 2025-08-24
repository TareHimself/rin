using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Graphics.Windows.Events;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Graphics_Windows_Events_CharacterEvent = Rin.Framework.Graphics.Windows.Events.CharacterEvent;
using Graphics_Windows_Events_CursorMoveEvent = Rin.Framework.Graphics.Windows.Events.CursorMoveEvent;
using Graphics_Windows_Events_ResizeEvent = Rin.Framework.Graphics.Windows.Events.ResizeEvent;
using Graphics_Windows_Events_ScrollEvent = Rin.Framework.Graphics.Windows.Events.ScrollEvent;

namespace Rin.Framework.Views.Window;

/// <summary>
///     A surface that displays views on a window
/// </summary>
public class WindowSurface : Surface
{
    private readonly IWindowRenderer _renderer;
    private bool _minimized;
    private Extent2D _size;

    public WindowSurface(IWindowRenderer renderer)
    {
        _renderer = renderer;
        _size = _renderer.GetRenderExtent();
    }

    public IWindow Window => _renderer.GetWindow();

    public IWindowRenderer GetRenderer()
    {
        return _renderer;
    }

    public override void Init()
    {
        base.Init();
        Window.OnCursorButton += OnMouseButton;
        Window.OnCursorMoved += OnMouseMove;
        Window.OnScroll += OnScroll;
        Window.OnKey += OnKeyboard;
        Window.OnCharacter += OnCharacter;
        _renderer.OnCollect += Collect;
        Window.OnResize += OnWindowResized;
        Window.OnCursorFocus += OnCursorFocus;
    }

    private void Collect(IGraphBuilder builder)
    {
        if (Stats.InitialCommandCount != 0) Stats = new FrameStats();
        if (BuildPasses(builder) is { } context) builder.AddPass(new CopySurfaceToSwapchain(context));
    }

    protected void OnWindowResized(ResizeEvent e)
    {
        _size = _renderer.GetRenderExtent();
        _minimized = _size.Width == 0 || _size.Height == 0;
        if (!_minimized) ReceiveResize(new ResizeSurfaceEvent(this, _size));
    }

    protected void OnKeyboard(KeyEvent e)
    {
        ReceiveKeyboard(new KeyboardSurfaceEvent(this, e.Key, e.State));
    }

    protected void OnCharacter(CharacterEvent e)
    {
        ReceiveCharacter(new CharacterSurfaceEvent(this, e.Data, e.Modifiers));
    }

    protected void OnMouseButton(CursorButtonEvent e)
    {
        switch (e.State)
        {
            case InputState.Released:
                ReceiveCursorUp(new CursorUpSurfaceEvent(this, e.Button, e.Position));
                break;
            case InputState.Pressed:
                ReceiveCursorDown(new CursorDownSurfaceEvent(this, e.Button, e.Position));
                break;
            case InputState.Repeat:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected void OnMouseMove(CursorMoveEvent e)
    {
        ReceiveCursorMove(new CursorMoveSurfaceEvent(this, e.Position));
    }

    protected void OnScroll(ScrollEvent e)
    {
        ReceiveScroll(new ScrollSurfaceEvent(this, e.Position, e.Delta));
    }

    protected void OnCursorFocus(FocusEvent e)
    {
        if (e.IsFocused)
            ReceiveCursorEnter(new CursorMoveSurfaceEvent(this, Window.GetCursorPosition()));
        else
            ReceiveCursorLeave();
    }

    public override void Dispose()
    {
        base.Dispose();
        Window.OnResize -= OnWindowResized;
        _renderer.OnCollect -= Collect;
        Window.OnCursorButton -= OnMouseButton;
        Window.OnCursorMoved -= OnMouseMove;
        Window.OnScroll -= OnScroll;
        Window.OnKey -= OnKeyboard;
        Window.OnCharacter -= OnCharacter;
        Window.OnCursorFocus -= OnCursorFocus;
    }

    public override Vector2 GetCursorPosition()
    {
        return Window.GetCursorPosition();
    }

    public override void SetCursorPosition(Vector2 position)
    {
        Window.SetCursorPosition(position);
    }

    public override void StartTyping(View view)
    {
        Window.StartTyping();
    }

    public override void StopTyping(View view)
    {
        Window.StopTyping();
    }

    public override Vector2 GetSize()
    {
        return new Vector2(_size.Width, _size.Height);
    }
}