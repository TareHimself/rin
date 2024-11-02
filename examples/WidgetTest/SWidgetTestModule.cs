using rin.Core;
using rin.Core.Animation;
using rin.Core.Extensions;
using rin.Widgets;
using rin.Widgets.Animation;
using rin.Widgets.Containers;
using rin.Widgets.Content;
using rin.Windows;

namespace WidgetTest;

[RuntimeModule(typeof(SWidgetsModule))]
public class SWidgetTestModule : RuntimeModule
{
    public void TestBlur()
    {
        var surf = SWidgetsModule.Get().GetWindowSurface();

        if (surf == null) return;

        var panel = surf.Add(new PanelContainer());

        var switcher = new SwitcherContainer();
        
        var imageSlot = (PanelContainerSlot?)panel.AddChild(
            new PanelContainerSlot
            {
                Child = switcher,
                MaxAnchor = 1.0f
            }
        );
        
        panel.AddChild(new PanelContainerSlot{
            Child = new BlurContainer
            {
                Tint = new Color(1.0f)
                {
                    R = 0.3f,
                    G = 0.3f,
                    B = 0.3f
                }
            },
            MinAnchor = 0.0f,
            MaxAnchor = 1.0f
        });

        var textSlot = (PanelContainerSlot?)panel.AddChild(new PanelContainerSlot
        {
            Child = new TextWidget("Background Blur Using Blit", 40),
            SizeToContent = true,
            Alignment = 0f,
            MinAnchor = 0f,
            MaxAnchor = 0f
        });

        var textInputSlot = panel.AddChild(new PanelContainerSlot
        {
            Child = new SizerContainer
            {
                Child = new TextInputWidget("Example Text",100),
                WidthOverride = 500
            },
            SizeToContent = true,
            Alignment = 0.5f,
            MinAnchor = 0.5f,
            MaxAnchor = 0.5f
        });
        
        if (textSlot == null || imageSlot == null || textInputSlot == null) return;

        var frames = 0;
        SRuntime.Get().OnTick += d =>
        {
            frames++;
            var txt = (TextWidget)textSlot.Child;
            txt.Content = $"Selected Index {switcher.SelectedIndex}";//$"Focused ${panel.Surface?.FocusedWidget}";
        };

        surf.Window.OnKey += (e) =>
        {
            

            var switcherSlots = switcher.GetNumSlots();
            if (switcherSlots > 0)
            {
                if (e is { State: KeyState.Pressed or KeyState.Repeat, Key: Key.Left })
                {
                    var newIndex = switcher.SelectedIndex - 1;
                    if (newIndex < 0)
                    {
                        newIndex = switcherSlots + newIndex;
                    }
                    switcher.SelectedIndex = newIndex;
                    return;
                }
                
                if (e is { State: KeyState.Pressed or KeyState.Repeat, Key: Key.Right })
                {
                    var newIndex = switcher.SelectedIndex + 1;
                    if (newIndex >= switcherSlots)
                    {
                        newIndex %= switcherSlots;
                    }
                    switcher.SelectedIndex = newIndex;
                    return;
                }
            }
            
            if (e is { State: KeyState.Pressed, Key: Key.F })
            {
                switcher.RotateTo(0, 360,2).Then().RotateTo(360, 0,2);
                return;
            }
            
            if (e is { State: KeyState.Pressed, Key: Key.Enter })
            {
                var p = Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true);
                foreach (var path in p)
                    switcher.AddChild(new FitterContainer
                    {
                        Child = new AsyncFileImage(path),
                        FittingMode = FitMode.Cover,
                        Clip = Clip.Bounds,
                        Padding = new Padding(100.0f)
                    });
            }
        };
    }


    public void TestClip()
    {
        var surf = SWidgetsModule.Get().GetWindowSurface();

        if (surf == null) return;

        var panel = surf.Add(new PanelContainer());

        var sizer = new SizerContainer
        {
            Child = new FitterContainer
            {
                Child = new AsyncFileImage(""),
                FittingMode = FitMode.Cover,
                Clip = Clip.Bounds
            },
            WidthOverride = 600,
            HeightOverride = 600
        };

        panel.AddChild(new PanelContainerSlot
        {
            Child = sizer,
            MinAnchor = 0.5f,
            MaxAnchor = 0.5f,
            Size = 200.0f,
            Alignment = 0.5f
        });
    }
    
    public void TestText()
    {
        var surf = SWidgetsModule.Get().GetWindowSurface();

        if (surf == null) return;

        var panel = surf.Add(new PanelContainer());

       
        var textSlot = panel.AddChild(new PanelContainerSlot
        {
            Child = new TextWidget("Test Text", 40),
            SizeToContent = true,
            Alignment = 0f,
            MinAnchor = 0f,
            MaxAnchor = 0f
        });
    }

    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        Console.WriteLine("CREATING WINDOW");
        SWindowsModule.Get().CreateWindow(500, 500, "Aerox Widget Test");
        
        TestBlur();
        //TestText();
    }
}