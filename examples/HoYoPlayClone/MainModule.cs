﻿using HoYoPlayClone.widgets;
using rin.Core;
using rin.Widgets;
using rin.Widgets.Containers;
using rin.Windows;

namespace HoYoPlayClone;

[RuntimeModule(typeof(SWidgetsModule))]
public class MainModule : RuntimeModule
{
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        SWindowsModule.Get().CreateWindow(1280,720,"HoYoPlay Clone",options: new WindowCreateOptions()
        {
            //Decorated = false,
        });


        if (SWidgetsModule.Get().GetWindowSurface() is { } surface)
        {
            surface.Add(new FlexBox
            {
                Axis = Axis.Row,
                Slots = [
                new FlexBoxSlot
                {
                    Child = new Sizer
                    {
                        WidthOverride = 80,
                        Child = new FlexBox
                        {
                        
                        }
                    },
                    Fit = CrossFit.Fill
                },
                new FlexBoxSlot
                {
                    Child = new Panel
                    {
                        Slots = [
                            new PanelSlot
                            {
                                Child = new AsyncWebCover(@"https://wallpaperswide.com/download/connected-wallpaper-3840x2160.jpg"),
                                MinAnchor = 0.0f,
                                MaxAnchor = 1.0f
                            }
                        ]
                    },
                    Flex = 1,
                    Fit = CrossFit.Fill
                }]
            });
        }
    }

    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);
    }
}