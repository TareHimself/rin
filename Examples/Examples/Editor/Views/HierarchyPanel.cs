using System.Numerics;
using Examples.Common.Views;
using Examples.Editor.Model;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;
using Rin.World;
using Rin.World.Transforms;

namespace Examples.Editor.Views;

/// <summary>
///     A filter box and a New entity button over the <see cref="HierarchyView" />.
/// </summary>
public static class HierarchyPanel
{
    public static IView Create(World world, EditorSelection selection, EntityNames names)
    {
        var tree = new HierarchyView(world, selection, names);
        var search = new SearchInputView { ForegroundColor = EditorTheme.Text };
        search.Edited += tree.SetFilter;
        search.FontSize = EditorTheme.FontSize;
        var searchIcon = new FontIconView { FontFamily = IconFont.Family, Icon = EditorIcons.Search, IconSize = 11f, Color = EditorTheme.Faint };
        var searchBox = new FieldBox(Ui.Row(
            Ui.Slot(searchIcon, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 8f), fit: CrossFit.Desired),
            Ui.Slot(search, 1f, align: CrossAlign.Center)), search);
        var newEntity = new ToolButton(EditorIcons.Plus, null, () =>
        {
            var entity = world.Spawn();
            world.SetPosition(entity, Vector3.Zero);
            selection.Select(entity);
        });

        return Ui.Column(
            Ui.Slot(Ui.Row(
                Ui.Slot(searchBox, 1f, align: CrossAlign.Center),
                Ui.Slot(Ui.Gap(0f, 6f), fit: CrossFit.Desired),
                Ui.Slot(newEntity, fit: CrossFit.Desired, align: CrossAlign.Center))),
            Ui.Slot(Ui.Gap(8f)),
            Ui.Slot(tree, 1f));
    }
}
