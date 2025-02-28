using System.Numerics;
using Facebook.Yoga;
using Rin.Engine.Views.Composite;

namespace Rin.Engine.Views.Layouts;

public enum AxisMode
{
    Percent,
    Absolute
}

public class YogaSlot : Slot
{
    private YogaNode _node = new();

    public YogaSlot(YogaLayout? layout = null) : base(layout)
    {
        _node.SetMeasureFunction(Measure);
    }

    public ref YogaNode Node => ref _node;

    private YogaSize Measure(YogaNode node,
        float width,
        YogaMeasureMode widthMode,
        float height,
        YogaMeasureMode heightMode)
    {
        switch (widthMode)
        {
            case YogaMeasureMode.Undefined:
                break;
            case YogaMeasureMode.Exactly:
                break;
            case YogaMeasureMode.AtMost:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(widthMode), widthMode, null);
        }


        return new YogaSize();
    }
}

public class YogaLayout(Axis axis, CompositeView container) : InfiniteChildrenLayout
{
    private YogaNode _root = new();
    public override CompositeView Container { get; }

    public override void Dispose()
    {
        throw new NotImplementedException();
    }

    public override ISlot MakeSlot(View view)
    {
        return new YogaSlot(this)
        {
            Child = view
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        throw new NotImplementedException();
    }

    public override Vector2 Apply(Vector2 availableSpace)
    {
        throw new NotImplementedException();
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return Vector2.Zero; //return _root
    }
}