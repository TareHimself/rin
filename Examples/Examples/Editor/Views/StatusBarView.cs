using Examples.Common.Views;
using Examples.Editor.Model;
using Rin.Core.Views;
using Rin.World;

namespace Examples.Editor.Views;

/// <summary>
///     A line along the bottom of the editor: the play state, how many entities there are, and what is selected.
/// </summary>
public sealed class StatusBarView : BarView
{
    public StatusBarView(PlaySession session, World world, EditorSelection selection, EntityNames names)
    {
        Color = EditorTheme.Panel;
        Padding = new Padding(14f, 6f);
        SetChild(new LiveTextView<(PlayState, int, Entity, string?)>(
            () => (session.State, world.EntityCount, selection.Entity, names.NameOf(selection.Entity)),
            status => Format(world, names, status.Item1, status.Item2, status.Item3))
        {
            FontSize = 12f,
            ForegroundColor = EditorTheme.Muted
        });
    }

    private static string Format(World world, EntityNames names, PlayState state, int entityCount, Entity selected)
    {
        var selection = world.IsAlive(selected) ? names.NameOf(selected) ?? $"#{selected.Id}" : "Nothing";
        return $"{state}   ·   {entityCount} entities   ·   {selection} selected";
    }
}
