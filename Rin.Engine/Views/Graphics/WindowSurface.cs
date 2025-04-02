using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Graphics.Windows.Events;
using Rin.Engine.Views.Events;
using CursorEvent = Rin.Engine.Graphics.Windows.Events.CursorEvent;
using Graphics_Windows_Events_CharacterEvent = Rin.Engine.Graphics.Windows.Events.CharacterEvent;
using Graphics_Windows_Events_CursorMoveEvent = Rin.Engine.Graphics.Windows.Events.CursorMoveEvent;
using Graphics_Windows_Events_ResizeEvent = Rin.Engine.Graphics.Windows.Events.ResizeEvent;
using Graphics_Windows_Events_ScrollEvent = Rin.Engine.Graphics.Windows.Events.ScrollEvent;

namespace Rin.Engine.Views.Graphics;

/// <summary>
///     A surface that displays views on a window
/// </summary>
public class WindowSurface : Surface
{
    private readonly IWindowRenderer _renderer;
    private bool _minimized;
    private Vector2<int> _size;

    public WindowSurface(IWindowRenderer renderer)
    {
        _renderer = renderer;
        _size = Window.GetPixelSize().Cast<int>();
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
        Window.OnScrolled += OnScroll;
        Window.OnKey += OnKeyboard;
        Window.OnCharacter += OnCharacter;
        _renderer.OnCollect += Collect;
        Window.OnResized += OnWindowResized;
        Window.OnCursorEnter += OnCursorEnter;
        Window.OnCursorLeave += OnCursorLeave;
    }

    private void Collect(IGraphBuilder builder)
    {
        if (Stats.InitialCommandCount != 0) Stats = new FrameStats();
        if (ComputePassInfo() is { } passInfo) builder.AddPass(new WindowSurfacePass(this, GetSize(), passInfo));
    }

    protected void OnWindowResized(Graphics_Windows_Events_ResizeEvent e)
    {
        _size = e.Size.Cast<int>();
        _minimized = _size.X == 0 || _size.Y == 0;
        if (!_minimized) ReceiveResize(new ResizeSurfaceEvent(this, _size.Clone()));
    }

    protected void OnKeyboard(KeyEvent e)
    {
        ReceiveKeyboard(new KeyboardSurfaceEvent(this, e.Key, e.State));
    }

    protected void OnCharacter(Graphics_Windows_Events_CharacterEvent e)
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

    protected void OnMouseMove(Graphics_Windows_Events_CursorMoveEvent e)
    {
        ReceiveCursorMove(new CursorMoveSurfaceEvent(this, e.Position));
    }

    protected void OnScroll(Graphics_Windows_Events_ScrollEvent e)
    {
        ReceiveScroll(new ScrollSurfaceEvent(this, e.Position, e.Delta));
    }

    protected void OnCursorEnter(CursorEvent e)
    {
        ReceiveCursorEnter(new CursorMoveSurfaceEvent(this, e.Position));
    }

    protected void OnCursorLeave(WindowEvent e)
    {
        ReceiveCursorLeave();
    }


    public override void Dispose()
    {
        base.Dispose();
        Window.OnResized -= OnWindowResized;
        _renderer.OnCollect -= Collect;
        Window.OnCursorButton -= OnMouseButton;
        Window.OnCursorMoved -= OnMouseMove;
        Window.OnScrolled -= OnScroll;
        Window.OnKey -= OnKeyboard;
        Window.OnCharacter -= OnCharacter;
        Window.OnCursorEnter -= OnCursorEnter;
        Window.OnCursorLeave -= OnCursorLeave;
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
        return new Vector2(_size.X, _size.Y);
    }
}