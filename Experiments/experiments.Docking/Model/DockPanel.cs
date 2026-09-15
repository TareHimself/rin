using Rin.Core.Views;

namespace experiments.Docking.Model;

/// <summary>
///     A single dockable panel: a title plus the user's content view.
///     The <see cref="Content" /> reference is stable for the lifetime of the panel — the
///     docking views re-home it as the tree changes, they never rebuild or dispose it
///     (until the panel is closed).
/// </summary>
public sealed class DockPanel(string id, string title, IView content)
{
    public string Id { get; } = id;
    public string Title { get; set; } = title;
    public IView Content { get; } = content;
}
