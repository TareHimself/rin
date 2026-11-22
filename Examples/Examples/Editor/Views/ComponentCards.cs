using Examples.Common.Views;
using Examples.Editor.Model;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.Editor.Views;

/// <param name="NeededBy">The names of the entity's components that require this one, which keep it from removal.</param>
/// <param name="IsHidden">A derived or runtime component, shown read-only under Show hidden and never removed here.</param>
public sealed record ComponentCard(
    ComponentAccess Access,
    bool IsCollapsed,
    bool IsMenuOpen,
    IReadOnlyList<string> NeededBy,
    bool IsHidden);

/// <summary>
///     A collapsible card per component: an arrow, its name, a lock while another component needs it, a menu with
///     Remove, and a row per field from <see cref="FieldRows" />.
/// </summary>
public static class ComponentCards
{
    private const float RowGap = 6f;
    private const float BodyInset = 4f;

    public static IView Create(ComponentCard card, Action toggleCollapsed, Action toggleMenu)
    {
        var slots = new List<FlexBoxSlot> { Ui.Slot(Header(card, toggleCollapsed, toggleMenu)) };
        if (card.IsMenuOpen) AddRow(slots, Menu(card));
        if (!card.IsCollapsed) AddFields(slots, card.Access);

        return new CardView
        {
            Color = card.IsHidden ? EditorTheme.Ground : EditorTheme.Card,
            BorderColor = EditorTheme.Border,
            BorderRadius = BorderRadius.All(EditorTheme.CardRadius),
            Padding = new Padding { Left = 6f, Top = 5f, Right = 6f, Bottom = card.IsCollapsed ? 5f : 12f },
            InitChild = Ui.Column(slots.ToArray())
        };
    }

    private static IView Header(ComponentCard card, Action toggleCollapsed, Action toggleMenu)
    {
        var info = card.Access.Info;
        var arrowIcon = card.IsCollapsed ? EditorIcons.Collapsed : EditorIcons.Expanded;
        var arrow = new ToolButton(arrowIcon, null, toggleCollapsed, ToolButtonStyle.Quiet) { Padding = new Padding(8f, 8f) };
        var title = Ui.Label(info.Type.Name, EditorTheme.FontSize, card.IsHidden ? EditorTheme.TextSoft : EditorTheme.Text);
        var row = Ui.Row(
            Ui.Slot(arrow, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(title, 1f, align: CrossAlign.Center));

        if (card.IsHidden)
        {
            row.Add(Ui.Slot(Pill(info.IsDerived ? "derived" : "runtime"), fit: CrossFit.Desired, align: CrossAlign.Center));
            row.Add(Ui.Slot(Ui.Gap(0f, 6f), fit: CrossFit.Desired));
            return row;
        }

        if (card.NeededBy.Count > 0) row.Add(Ui.Slot(LockBadge(), fit: CrossFit.Desired, align: CrossAlign.Center));
        row.Add(Ui.Slot(Ui.Gap(0f, 2f), fit: CrossFit.Desired));
        row.Add(Ui.Slot(new ToolButton(EditorIcons.Ellipsis, null, toggleMenu, ToolButtonStyle.Quiet), fit: CrossFit.Desired,
            align: CrossAlign.Center));
        return row;
    }

    private static IView LockBadge()
    {
        var icon = new FontIconView { FontFamily = IconFont.Family, Icon = EditorIcons.Lock, IconSize = 10f, Color = EditorTheme.LockText };
        return new CardView
        {
            Color = EditorTheme.LockFill,
            BorderRadius = BorderRadius.All(EditorTheme.FieldRadius),
            Padding = new Padding(6f, 5f),
            InitChild = icon
        };
    }

    private static IView Pill(string text) => new CardView
    {
        Color = EditorTheme.Pill,
        BorderRadius = BorderRadius.All(EditorTheme.FieldRadius),
        Padding = new Padding(7f, 2f),
        InitChild = Ui.Label(text, 11f, EditorTheme.Muted)
    };

    private static IView Menu(ComponentCard card)
    {
        if (card.NeededBy.Count > 0)
            return Ui.Label($"Needed by {string.Join(", ", card.NeededBy)}", EditorTheme.FontSize, EditorTheme.Muted);

        var remove = new ToolButton(null, "Remove component", card.Access.Remove);
        return Ui.Row(Ui.Slot(remove, fit: CrossFit.Desired));
    }

    private static void AddFields(List<FlexBoxSlot> slots, ComponentAccess access)
    {
        if (access.Info.IsTag)
        {
            AddRow(slots, Ui.Label("Tag, no fields", EditorTheme.FontSize, EditorTheme.Faint));
            return;
        }

        foreach (var row in FieldRows.Of(access.Info))
            if (row.Kind != FieldRowKind.ProcessLocal)
                AddRow(slots, FieldRowViews.Create(row, access));
    }

    private static void AddRow(List<FlexBoxSlot> slots, IView row)
    {
        slots.Add(Ui.Slot(Ui.Gap(RowGap)));
        slots.Add(Ui.Slot(new RectView
        {
            Color = Ui.Clear,
            Padding = new Padding { Left = BodyInset + 2f, Right = BodyInset },
            InitChild = row
        }));
    }
}
