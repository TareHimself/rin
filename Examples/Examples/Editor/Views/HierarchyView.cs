using System.Numerics;
using Examples.Common.Views;
using Examples.Editor.Model;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using Rin.World;

namespace Examples.Editor.Views;

/// <summary>
///     Every entity as an indented tree. Clicking a row selects it, and clicking its arrow folds its children away.
/// </summary>
/// <remarks>
///     The tree is re-read a few times a second and its rows rebuilt only when it changed, since entities spawn and
///     move between parents while the world plays.
/// </remarks>
public sealed class HierarchyView : ScrollListView
{
    private const float RefreshSeconds = 0.25f;

    private readonly World _world;
    private readonly EditorSelection _selection;
    private readonly EntityNames _names;
    private readonly List<Entity> _scratch = [];
    private readonly HashSet<Entity> _collapsed = [];
    private readonly List<HierarchyRow> _rows = [];
    private readonly List<HierarchyRow> _next = [];
    private readonly List<RowView> _rowViews = [];
    private string _filter = string.Empty;
    private float _sinceRefresh = RefreshSeconds;

    public HierarchyView(World world, EditorSelection selection, EntityNames names)
    {
        _world = world;
        _selection = selection;
        _names = names;
        Axis = Axis.Column;
        BarColor = EditorTheme.Faint;
        selection.Changed += HighlightSelection;
    }

    public void SetFilter(string filter)
    {
        _filter = filter.Trim();
        Refresh();
    }

    public override void Update(float deltaTime)
    {
        _sinceRefresh += deltaTime;
        if (_sinceRefresh >= RefreshSeconds) Refresh();
        base.Update(deltaTime);
    }

    private void Refresh()
    {
        _sinceRefresh = 0f;
        HierarchyRows.Build(_world, _names, _scratch, _collapsed, _filter, _next);
        if (_next.SequenceEqual(_rows)) return;

        _rows.Clear();
        _rows.AddRange(_next);
        foreach (var view in _rowViews) Remove(view);
        _rowViews.Clear();
        foreach (var row in _rows)
        {
            var view = new RowView(row, () => _selection.Select(row.Entity), () => ToggleCollapsed(row.Entity));
            _rowViews.Add(view);
            Add(new ListSlot { Child = view, Fit = CrossFit.Fill });
        }

        HighlightSelection();
    }

    private void ToggleCollapsed(Entity entity)
    {
        if (!_collapsed.Remove(entity)) _collapsed.Add(entity);
        Refresh();
    }

    private void HighlightSelection()
    {
        foreach (var view in _rowViews) view.SetSelected(view.Entity == _selection.Entity);
    }

    private sealed class RowView : ButtonView
    {
        private bool _isSelected;

        private const float Indent = 16f;
        private const float ArrowWidth = 20f;

        private readonly IView _content;

        public RowView(HierarchyRow row, Action onSelected, Action onToggled)
        {
            Entity = row.Entity;
            Padding = new Padding { Left = 4f + row.Depth * Indent, Right = 8f, Top = 7f, Bottom = 7f };
            BorderRadius = BorderRadius.All(7f);
            Color = Ui.Clear;
            _content = Ui.Row(
                Controls.Fixed(Arrow(row, onToggled), ArrowWidth),
                Ui.Slot(Ui.Label(row.Label, EditorTheme.FontSize, EditorTheme.Text), fit: CrossFit.Desired,
                    align: CrossAlign.Center));
            SetChild(_content);
            OnReleased += (_, _) =>
            {
                if (IsHovered) onSelected();
            };
        }

        public Entity Entity { get; }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            Repaint();
        }

        protected override void OnCursorEnter(Rin.Core.Views.Events.CursorMoveSurfaceEvent e)
        {
            base.OnCursorEnter(e);
            Repaint();
        }

        protected override void OnCursorLeave()
        {
            base.OnCursorLeave();
            Repaint();
        }

        private void Repaint() =>
            Color = _isSelected ? EditorTheme.AccentFill : IsHovered ? EditorTheme.Hover : Ui.Clear;

        private static IView Arrow(HierarchyRow row, Action onToggled)
        {
            if (!row.HasChildren) return Ui.Gap(0f);

            var icon = row.IsExpanded ? EditorIcons.Expanded : EditorIcons.Collapsed;
            return new ToolButton(icon, null, onToggled, ToolButtonStyle.Quiet) { Padding = new Padding(5f, 2f) };
        }

        // A row fills the panel's width, so the highlight and the click area span it.
        protected override Vector2 ArrangeContent(in Vector2 availableSpace)
        {
            var contentSize = _content.Layout(availableSpace);
            var width = float.IsFinite(availableSpace.X) ? availableSpace.X : contentSize.X;
            return new Vector2(width, contentSize.Y);
        }
    }
}
