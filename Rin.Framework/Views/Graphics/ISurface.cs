using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Math;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics.CommandHandlers;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Views.Graphics.Passes;

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
    public SurfaceContext? BuildPasses(IGraphBuilder builder);

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
}