using System.Runtime.InteropServices;
using Examples.Common.Views;
using Examples.Editor.Model;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using Rin.World;

namespace Examples.Editor.Views;

/// <summary>
///     The selected entity's components as cards, an Add component picker that brings each component's requirements
///     with it, and the derived and runtime components behind Show hidden.
/// </summary>
/// <remarks>
///     The cards are rebuilt when the selection, the entity's set of components or a toggle changes, checked before the
///     cards update so none of them reads a component that is gone.
/// </remarks>
public sealed class InspectorView : ScrollListView
{
    private const float SectionGap = 8f;

    private readonly World _world;
    private readonly EditorSelection _selection;
    private readonly EntityNames _names;
    private readonly List<ComponentId> _shownComponents = [];
    private readonly HashSet<ComponentId> _collapsed = [];
    private readonly List<ComponentId> _scratch = [];
    private Entity _shownEntity;
    private bool _isShowingEntity;
    private ComponentId? _openMenu;
    private bool _isEntityMenuOpen;
    private bool _isAdding;
    private bool _showHidden;
    private bool _isStale = true;

    public InspectorView(World world, EditorSelection selection, EntityNames names)
    {
        _world = world;
        _selection = selection;
        _names = names;
        Axis = Axis.Column;
        BarColor = EditorTheme.Faint;
        FloatingBar = false;
        selection.Changed += () =>
        {
            _openMenu = null;
            _isEntityMenuOpen = false;
            _isAdding = false;
            _isStale = true;
        };
    }

    public override void Update(float deltaTime)
    {
        if (_isStale || !IsShowingCurrentEntity()) Rebuild();
        base.Update(deltaTime);
    }

    private bool IsShowingCurrentEntity()
    {
        var entity = _selection.Entity;
        var isAlive = _world.IsAlive(entity);
        if (isAlive != _isShowingEntity || entity != _shownEntity) return false;

        return !isAlive || _world.ComponentsOf(entity).SequenceEqual(CollectionsMarshal.AsSpan(_shownComponents));
    }

    private static bool IsHidden(ComponentInfo info) => !info.IsSaved || info.IsDerived;

    private void Rebuild()
    {
        _isStale = false;
        foreach (var slot in GetSlots()) Remove(slot.Child);

        var entity = _shownEntity = _selection.Entity;
        _shownComponents.Clear();
        _isShowingEntity = _world.IsAlive(entity);
        if (!_isShowingEntity)
        {
            AddSection(Ui.Label("Select an entity in the hierarchy.", EditorTheme.FontSize, EditorTheme.Muted));
            return;
        }

        foreach (var id in _world.ComponentsOf(entity)) _shownComponents.Add(id);
        AddSection(EntityHeader(entity));
        if (_isEntityMenuOpen) AddSection(EntityMenu(entity));
        AddCards(entity, hidden: false);
        AddAddComponent(entity);
        AddHiddenToggle(entity);
    }

    private IView EntityHeader(Entity entity)
    {
        var id = new CardView
        {
            Color = EditorTheme.Pill,
            BorderRadius = BorderRadius.All(EditorTheme.FieldRadius),
            Padding = new Padding(8f, 3f),
            InitChild = Ui.Label($"#{entity.Id}", 12f, EditorTheme.Muted)
        };
        var menu = new ToolButton(EditorIcons.Ellipsis, null, () => Toggle(ref _isEntityMenuOpen));
        return Ui.Row(
            Ui.Slot(new NameInputView(_world, _names, entity), 1f, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 8f), fit: CrossFit.Desired),
            Ui.Slot(id, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 8f), fit: CrossFit.Desired),
            Ui.Slot(menu, fit: CrossFit.Desired, align: CrossAlign.Center));
    }

    private IView EntityMenu(Entity entity)
    {
        var despawn = new ToolButton(null, "Despawn entity", () => _world.Despawn(entity), ToolButtonStyle.Outline);
        return Ui.Row(Ui.Slot(despawn, fit: CrossFit.Desired));
    }

    private void AddCards(Entity entity, bool hidden)
    {
        foreach (var id in _shownComponents)
        {
            if (IsHidden(_world.Schema[id]) != hidden) continue;

            var card = new ComponentCard(new ComponentAccess(_world, entity, id), _collapsed.Contains(id), _openMenu == id,
                NeededBy(entity, id), hidden);
            AddSection(ComponentCards.Create(card, () => ToggleCollapsed(id), () => ToggleMenu(id)));
        }
    }

    private List<string> NeededBy(Entity entity, ComponentId id)
    {
        ComponentRequirements.CollectDependents(_world, entity, id, _scratch);
        return _scratch.Select(dependent => _world.Schema[dependent].Type.Name).ToList();
    }

    private void AddAddComponent(Entity entity)
    {
        var icon = _isAdding ? EditorIcons.Close : EditorIcons.Plus;
        AddSection(new ToolButton(icon, _isAdding ? "Cancel" : "Add component", () => Toggle(ref _isAdding),
            ToolButtonStyle.Outline) { IsFullWidth = true, IsCentered = true }, 6f);
        if (_isAdding) AddSection(new ComponentPickerView(_world, entity, () => _isAdding = false));
    }

    private void AddHiddenToggle(Entity entity)
    {
        var hiddenCount = _shownComponents.Count(id => IsHidden(_world.Schema[id]));
        if (hiddenCount == 0) return;

        AddSection(new BarView { Color = EditorTheme.Border, Padding = new Padding(0f, 0.5f) }, 10f);
        AddSection(Ui.Row(
            Ui.Slot(new CheckBoxView(() => _showHidden, () => Toggle(ref _showHidden)), fit: CrossFit.Desired,
                align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 8f), fit: CrossFit.Desired),
            Ui.Slot(Ui.Label($"Show hidden ({hiddenCount})", EditorTheme.FontSize, EditorTheme.Muted), fit: CrossFit.Desired,
                align: CrossAlign.Center)));
        if (_showHidden) AddCards(entity, hidden: true);
    }

    private void ToggleCollapsed(ComponentId id)
    {
        if (!_collapsed.Remove(id)) _collapsed.Add(id);
        _isStale = true;
    }

    private void ToggleMenu(ComponentId id)
    {
        _openMenu = _openMenu == id ? null : id;
        _isStale = true;
    }

    private void Toggle(ref bool flag)
    {
        flag = !flag;
        _isStale = true;
    }

    private void AddSection(IView view, float gapAfter = SectionGap)
    {
        Add(new ListSlot { Child = view, Fit = CrossFit.Fill });
        Add(new ListSlot { Child = Ui.Gap(gapAfter) });
    }
}
