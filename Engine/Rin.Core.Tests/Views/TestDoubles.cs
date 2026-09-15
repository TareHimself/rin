using System.Numerics;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Tests.Views;

/// <summary>Minimal <see cref="IApplication" /> so <c>View</c> construction (AnimationRunner) works headlessly.</summary>
internal sealed class FakeApplication : IApplication
{
    public float TimeSeconds => 0f;
    public float LastDeltaSeconds => 0f;
    public Dispatcher MainDispatcher { get; } = new();
    public Dispatcher RenderDispatcher { get; } = new();

    public event Action? OnPreUpdate;
    public event Action<float>? OnUpdate;
    public event Action? OnPostUpdate;
    public event Action? OnCollect;
    public event Action? OnPreRender;
    public event Action? OnRender;
    public event Action? OnPostRender;

    public IGraphicsModule CreateGraphicsModule() => throw new NotSupportedException();
    public IAudioModule CreateAudioModule() => throw new NotSupportedException();
    public IViewsModule CreateViewsModule() => throw new NotSupportedException();
    public void Run() => throw new NotSupportedException();
    public void RequestExit() { }
    public string[] SelectFile(string title = "Select File's", bool multiple = false, string filter = "") => [];
    public string[] SelectPath(string title = "Select Path's", bool multiple = false) => [];

    public void Dispose()
    {
        _ = OnPreUpdate;
        _ = OnUpdate;
        _ = OnPostUpdate;
        _ = OnCollect;
        _ = OnPreRender;
        _ = OnRender;
        _ = OnPostRender;
    }
}

/// <summary>Inert <see cref="ISurface" /> — just an identity to hand views so <c>Surface</c> gets set.</summary>
internal sealed class FakeSurface : ISurface
{
    public IView? FocusedView => null;
    public event Action<CursorUpSurfaceEvent>? OnCursorUp;

    public Vector2 GetCursorPosition() => Vector2.Zero;
    public void SetCursorPosition(Vector2 position) { }
    public void StartTyping(View view) { }
    public void StopTyping(View view) { }
    public void Init() { }
    public Vector2 GetSize() => new(1000f);
    public void ClearFocus() { }
    public bool RequestFocus(IView requester) => false;
    public CommandList? CollectCommands() => null;
    public void ReceiveCursorEnter(CursorMoveSurfaceEvent e) { }
    public void ReceiveCursorLeave() { }
    public void ReceiveResize(ResizeSurfaceEvent e) { }
    public void ReceiveCursorDown(CursorDownSurfaceEvent e) { }
    public void ReceiveCursorUp(CursorUpSurfaceEvent e) => OnCursorUp?.Invoke(e);
    public void ReceiveCursorMove(CursorMoveSurfaceEvent e) { }
    public void ReceiveScroll(ScrollSurfaceEvent e) { }
    public void ReceiveCharacter(CharacterSurfaceEvent e) { }
    public void ReceiveKeyboard(KeyboardSurfaceEvent e) { }
    public T Add<T>() where T : IView, new() => throw new NotSupportedException();
    public T Add<T>(T view) where T : IView => throw new NotSupportedException();
    public bool Remove(IView view) => false;
    public void OnViewLayoutInvalidated(IView view) { }
    public void ForceLayout() { }
    public void Update(float deltaTime) { }
    public void Dispose() { }
}
