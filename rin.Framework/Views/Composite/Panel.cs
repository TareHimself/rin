using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;



/// <summary>
///     A container that draws children based on the settings provided in <see cref="PanelSlot" /> . Intended use is for
///     dock-able layouts or as a root for a collection of widgets
/// </summary>
public class Panel : MultiSlotCompositeView<PanelSlot>
{
    private readonly PanelLayout _layout;


    public Panel() : base()
    {
        _layout = new PanelLayout(this);
    }
    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
        return _layout.Apply(availableSpace);
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        if (_layout.FindSlot(child) is { } slot)
        {
            _layout.OnSlotUpdated(slot);
        }
    }
    
    public override void OnChildAdded(View child)
    {
        if (_layout.FindSlot(child) is { } slot)
        {
            _layout.OnSlotUpdated(slot);
        }
    }

    public override IEnumerable<ISlot> GetSlots() => _layout.GetSlots();

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }

    public override int SlotCount => _layout.SlotCount;
    public override bool Add(View child) => _layout.Add(child);
    public override bool Add(PanelSlot slot) => _layout.Add(slot);
    public override bool Remove(View child) => _layout.Remove(child);
}