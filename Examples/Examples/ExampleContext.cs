using Rin.Core.Graphics.Windows;
using Rin.Core.Graphics.Windows.Events;
using Rin.Core.Views;
using Rin.Core.Views.Window;

namespace Examples;

/// <summary>
///     What a running example works with. Views and handlers added through it are removed when the example stops.
/// </summary>
public sealed class ExampleContext(ExampleHost application, IWindowSurface surface, string[] args)
{
    private readonly List<IView> _views = [];
    private readonly List<Action> _undo = [];
    private readonly List<IWindow> _children = [];

    public ExampleHost Application => application;

    public IWindowSurface Surface => surface;

    public string[] Args => args;

    public IWindow Window => surface.Window;

    /// <summary>
    ///     False once the example has stopped. Work that finishes later checks this before using the example.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    internal event Action? ViewAdded;

    public T Add<T>(T view) where T : IView
    {
        if (!IsActive)
        {
            view.Dispose();
            return view;
        }

        surface.Add(view);
        _views.Add(view);
        ViewAdded?.Invoke();
        return view;
    }

    public void OnUpdate(Action<float> handler)
    {
        if (!IsActive) return;
        application.OnUpdate += handler;
        _undo.Add(() => application.OnUpdate -= handler);
    }

    public void OnKey(Action<KeyEvent> handler)
    {
        if (!IsActive) return;
        Window.OnKey += handler;
        _undo.Add(() => Window.OnKey -= handler);
    }

    public void OnDrop(Action<DropEvent> handler)
    {
        if (!IsActive) return;
        Window.OnDrop += handler;
        _undo.Add(() => Window.OnDrop -= handler);
    }

    internal void AddChild(IWindowSurface childSurface)
    {
        var window = childSurface.Window;
        _children.Add(window);
        window.OnDispose += () => _children.Remove(window);
    }

    internal void Release()
    {
        IsActive = false;
        foreach (var child in _children.ToArray()) child.Dispose();
        foreach (var undo in _undo) undo();
        _undo.Clear();

        surface.ClearFocus();
        foreach (var view in _views)
        {
            surface.Remove(view);
            view.Dispose();
        }

        _views.Clear();
    }
}
