using Examples.Common.Views;
using Examples.Editor.Model;
using Rin.Core;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;
using Rin.World;
using Rin.World.Editing;

namespace Examples.Editor.Views;

/// <summary>
///     A search box over the editable components an entity lacks, each naming the requirements it would bring along.
/// </summary>
/// <remarks>
///     Typing refreshes only the list, so the search box keeps its focus.
/// </remarks>
public sealed class ComponentPickerView : CardView
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly Action _onAdded;
    private readonly ListView _choices = new(Axis.Column);
    private readonly List<ComponentId> _scratch = [];
    private string _filter = string.Empty;

    public ComponentPickerView(World world, Entity entity, Action onAdded)
    {
        _world = world;
        _entity = entity;
        _onAdded = onAdded;
        var search = new SearchInputView { ForegroundColor = EditorTheme.Text };
        search.Edited += text =>
        {
            _filter = text.Trim();
            RefreshChoices();
        };
        search.FontSize = EditorTheme.FontSize;

        Color = EditorTheme.Card;
        BorderColor = EditorTheme.BorderStrong;
        BorderRadius = BorderRadius.All(EditorTheme.CardRadius);
        Padding = new Padding(8f);
        SetChild(Ui.Column(
            Ui.Slot(new FieldBox(search, search)),
            Ui.Slot(Ui.Gap(4f)),
            Ui.Slot(_choices),
            Ui.Slot(Ui.Gap(4f)),
            Ui.Slot(Ui.Label("Requirements the entity lacks are added with it.", 11f, EditorTheme.Muted))));
        RefreshChoices();
    }

    private void RefreshChoices()
    {
        foreach (var slot in _choices.GetSlots()) _choices.Remove(slot.Child);

        var schema = _world.Schema;
        for (var i = 0; i < schema.Count; i++)
        {
            var id = new ComponentId(i);
            var info = schema[id];
            if (!ComponentJson.IsEditable(info) || _world.Has(_entity, id)) continue;
            if (!info.Type.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase)) continue;

            _choices.Add(new ListSlot { Child = Choice(id), Fit = CrossFit.Fill });
        }
    }

    private IView Choice(ComponentId id)
    {
        ComponentRequirements.CollectMissing(_world, _entity, id, _scratch);
        var brings = _scratch.Count == 0
            ? null
            : "+ " + string.Join(", ", _scratch.Select(required => _world.Schema[required].Type.Name));
        return new ToolButton(null, _world.Schema[id].Type.Name, () => AddComponent(id), ToolButtonStyle.Quiet, brings)
        {
            IsFullWidth = true,
            Padding = new Padding(8f, 7f)
        };
    }

    // Adding changes the entity's components, so the inspector rebuilds without this picker on its next update.
    private void AddComponent(ComponentId id) =>
        IApplication.Get().MainDispatcher.Enqueue(() =>
        {
            ComponentRequirements.AddWithRequirements(_world, _entity, id, _scratch);
            _onAdded();
        });
}
