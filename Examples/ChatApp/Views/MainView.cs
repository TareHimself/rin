using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Layouts;
using Rect = Rin.Engine.Views.Composite.Rect;

namespace ChatApp.Views;

public class MainView : FlexBox
{
    public MainView()
    {
        Axis = Axis.Row;
        Slots =
        [
            new FlexBoxSlot
            {
                Child = new SidePanel(),
                Fit = CrossFit.Fill
            },
            new FlexBoxSlot
            {
                Child = new ChatView(),
                Flex = 1,
                Fit = CrossFit.Fill
            }
        ];
    }
}