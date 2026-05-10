using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Views.Events;

namespace Rin.Framework.Views.Graphics;

public interface ISurface : IDisposable, IUpdatable
{
    public IView? FocusedView { get; }

    public Vector2 GetCursorPosition();

    public void SetCursorPosition(Vector2 position);

    public void StartTyping(View view);
    public void StopTyping(View view);
    public event Action<CursorUpSurfaceEvent>? OnCursorUp;

    public void Init();

    public Vector2 GetSize();

    public void ClearFocus();

    public bool RequestFocus(IView requester);

    [PublicAPI]
    public CommandList? CollectCommands();

    public void ReceiveCursorEnter(CursorMoveSurfaceEvent e);

    public void ReceiveCursorLeave();

    public void ReceiveResize(ResizeSurfaceEvent e);

    public void ReceiveCursorDown(CursorDownSurfaceEvent e);

    public void ReceiveCursorUp(CursorUpSurfaceEvent e);

    public void ReceiveCursorMove(CursorMoveSurfaceEvent e);

    public void ReceiveScroll(ScrollSurfaceEvent e);

    public void ReceiveCharacter(CharacterSurfaceEvent e);

    public void ReceiveKeyboard(KeyboardSurfaceEvent e);

    public T Add<T>() where T : IView, new();

    public T Add<T>(T view) where T : IView;

    public bool Remove(IView view);

    public void OnViewLayoutInvalidated(IView view);
    public void ForceLayout();
}