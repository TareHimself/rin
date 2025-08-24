﻿using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Layouts;

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
                Child = new Rect
                {
                    Child = new ChatView()
                },
                Flex = 1,
                Fit = CrossFit.Fill
            }
        ];
    }
}