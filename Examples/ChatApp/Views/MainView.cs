using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;

namespace ChatApp.Views;

public class MainView : FlexBoxView
{
    public MainView()
    {
        Axis = Axis.Row;
        InitSlots =
        [
            new FlexBoxSlot
            {
                Child = new SidePanel(),
                Fit = CrossFit.Fill
            },
            new FlexBoxSlot
            {
                Child = new RectView
                {
                    InitChild = new ChatView()
                },
                Flex = 1,
                Fit = CrossFit.Fill
            }
        ];
    }
}