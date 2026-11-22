using System.Numerics;
using Examples.Common.Views;
using Examples.Editor.Model;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;
using Rin.World;

namespace Examples.Editor.Views;

/// <summary>
///     The view for one <see cref="FieldRow" />: its label in a fixed column, then an editor chosen by the row's kind.
/// </summary>
public static class FieldRowViews
{
    private const float IndentWidth = 12f;
    private const float LaneGap = 4f;
    private const float AxisFontSize = 11f;
    private const float FieldLabelTop = 6f;

    // Labels sit at the top, level with a field's first line, so an open dropdown under them doesn't move them.
    public static IView Create(FieldRow row, ComponentAccess access)
    {
        var label = LabelOf(row, access);
        label.Padding = new Padding { Left = row.Depth * IndentWidth, Top = LabelTopOf(row) };
        if (EditorOf(row, access) is not { } editor)
            return Ui.Row(Ui.Slot(label, fit: CrossFit.Desired));

        return Ui.Row(
            Ui.Slot(new SizerView { WidthOverride = EditorTheme.LabelWidth, InitChild = label }, fit: CrossFit.Desired),
            Ui.Slot(editor, 1f));
    }

    private static float LabelTopOf(FieldRow row) => row.Kind switch
    {
        FieldRowKind.Bool or FieldRowKind.Group or FieldRowKind.Fixed => 0f,
        _ => FieldLabelTop
    };

    // A single number has no lane label, so the row label is what drags it.
    private static TextBoxView LabelOf(FieldRow row, ComponentAccess access)
    {
        if (row is { Kind: FieldRowKind.Number, IsReadOnly: false, Lanes: [{ Label: "" } lane] })
            return new ScrubLabelView(row.Label, () => access.ReadLane(lane), value => access.WriteLane(lane, value),
                FieldLanes.IsInteger(lane.Kind)) { FontSize = EditorTheme.FontSize };

        var color = row.Kind == FieldRowKind.Group ? EditorTheme.TextSoft : EditorTheme.Muted;
        return Ui.Label(row.Label, EditorTheme.FontSize, color);
    }

    private static IView? EditorOf(FieldRow row, ComponentAccess access) => row.Kind switch
    {
        FieldRowKind.Number => NumberEditor(row, access),
        FieldRowKind.Rotation => RotationEditor(row, access),
        FieldRowKind.Bool => BoolBox(row, access),
        FieldRowKind.Enum => EnumDropdown(row, access),
        FieldRowKind.Entity => EntityBox(row, access),
        FieldRowKind.Fixed => Ui.Label(row.Type.Kind.ToString(), EditorTheme.FontSize, EditorTheme.Faint),
        _ => null
    };

    private static IView NumberEditor(FieldRow row, ComponentAccess access)
    {
        var lanes = new List<LaneEditor>(row.Lanes.Length);
        foreach (var lane in row.Lanes)
            lanes.Add(new LaneEditor(lane.Label, () => access.ReadLane(lane), value => access.WriteLane(lane, value),
                FieldLanes.IsInteger(lane.Kind)));
        return LaneRow(lanes, row.IsReadOnly);
    }

    private static IView RotationEditor(FieldRow row, ComponentAccess access)
    {
        var rotation = new RotationLanes(() => access.ReadValue<Quaternion>(row.Offset),
            value => access.WriteValue(row.Offset, value));
        var lanes = new List<LaneEditor>(RotationLanes.Labels.Length);
        for (var axis = 0; axis < RotationLanes.Labels.Length; axis++)
        {
            var captured = axis;
            lanes.Add(new LaneEditor(RotationLanes.Labels[axis], () => rotation.Get(captured),
                value => rotation.Set(captured, (float)value), false));
        }

        return LaneRow(lanes, row.IsReadOnly);
    }

    private static IView LaneRow(List<LaneEditor> lanes, bool isReadOnly)
    {
        var slots = new List<FlexBoxSlot>();
        foreach (var lane in lanes)
        {
            if (slots.Count > 0) slots.Add(Ui.Slot(Ui.Gap(0f, LaneGap), fit: CrossFit.Desired));
            slots.Add(Ui.Slot(Lane(lane, isReadOnly), 1f));
        }

        return Ui.Row(slots.ToArray());
    }

    // The axis letter sits inside the box, as in the design, and dragging it scrubs the value.
    private static IView Lane(LaneEditor lane, bool isReadOnly)
    {
        var input = new NumberInputView(lane.Get, lane.Set, lane.IsInteger, isReadOnly)
        {
            FontSize = EditorTheme.FontSize,
            ForegroundColor = isReadOnly ? EditorTheme.Muted : EditorTheme.Text
        };
        if (lane.Label.Length == 0) return new FieldBox(input, input);

        TextBoxView handle = isReadOnly
            ? Ui.Label(lane.Label, AxisFontSize, EditorTheme.Faint)
            : new ScrubLabelView(lane.Label, lane.Get, lane.Set, lane.IsInteger)
            {
                FontSize = AxisFontSize,
                RestColor = AxisColorOf(lane.Label)
            };
        // The letter's padding fills the box's left edge, so the drag target is wider than the glyph.
        handle.Padding = new Padding { Left = 8f, Right = 6f, Top = 7f, Bottom = 7f };
        var content = Ui.Row(
            Ui.Slot(handle, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(input, 1f, align: CrossAlign.Center));
        return new FieldBox(content, input) { Padding = new Padding { Right = 8f } };
    }

    // Scrubbing is the same gesture on every lane, so the color only says which axis it moves.
    private static Color AxisColorOf(string label) => label switch
    {
        "X" or "P" => EditorTheme.AxisX,
        "Y" => EditorTheme.AxisY,
        "Z" or "R" => EditorTheme.AxisZ,
        _ => EditorTheme.Faint
    };

    private static IView BoolBox(FieldRow row, ComponentAccess access)
    {
        Action? toggle = row.IsReadOnly
            ? null
            : () => access.WriteValue(row.Offset, !access.ReadValue<bool>(row.Offset));
        var box = new CheckBoxView(() => access.ReadValue<bool>(row.Offset), toggle);
        return Ui.Row(Ui.Slot(box, fit: CrossFit.Desired, align: CrossAlign.Center));
    }

    private static IView EnumDropdown(FieldRow row, ComponentAccess access)
    {
        var choices = new EnumChoices(row.Type);
        Action<ulong>? choose = row.IsReadOnly
            ? null
            : value => choices.Write(access.Write(row.Offset, row.Type.Size), value);
        return new DropdownView<ulong>(() => choices.Read(access.Read(row.Offset, row.Type.Size)), choices.Values,
            choices.NameOf, choose);
    }

    private static IView EntityBox(FieldRow row, ComponentAccess access)
    {
        var text = new LiveTextView<Entity>(() => access.ReadValue<Entity>(row.Offset),
            entity => entity.IsValid ? $"#{entity.Id}" : "none")
        {
            FontSize = EditorTheme.FontSize,
            ForegroundColor = EditorTheme.Muted
        };
        return new FieldBox(text, null);
    }

    private readonly record struct LaneEditor(string Label, Func<double> Get, Action<double> Set, bool IsInteger);
}
