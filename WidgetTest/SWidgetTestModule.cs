using aerox.Runtime;
using aerox.Runtime.Extensions;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Containers;
using aerox.Runtime.Widgets.Content;
using aerox.Runtime.Windows;

namespace WidgetTest;

[RuntimeModule(typeof(SWidgetsModule))]
public class SWidgetTestModule : RuntimeModule
{
    public void TestBlur()
    {
        var surf = SWidgetsModule.Get().GetWindowSurface();

        if (surf == null) return;

        var panel = surf.Add(new Panel());

        var switcher = new Switcher();
        
        var imageSlot = (PanelSlot?)panel.AddChild(
            new PanelSlot(switcher)
            {
                MaxAnchor = 1.0f
            }
        );
        
        panel.AddChild(new PanelSlot(new BackgroundBlur
        {
            Tint = new Color(1.0f)
            {
                R = 0.3f,
                G = 0.3f,
                B = 0.3f
            }
        }){
            MinAnchor = 0.0f,
            MaxAnchor = 1.0f
        });

        var textSlot = (PanelSlot?)panel.AddChild(new PanelSlot(new Text("Background Blur Using Blit", 40))
        {
            SizeToContent = true,
            Alignment = 0f,
            MinAnchor = 0f,
            MaxAnchor = 0f
        });

        var textInputSlot = panel.AddChild(new PanelSlot(new Sizer(new TextBox("Example Text",100))
        {
            WidthOverride = 500
        })
        {
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
            var txt = (Text)textSlot.GetWidget();
            txt.Content = $"Focused ${panel.Surface?.FocusedWidget}";
        };

        surf.Window.OnKey += (e) =>
        {
            if (e is { State: KeyState.Pressed, Key: Key.Left })
            {
                if (switcher.SelectedIndex - 1 < 0) return;
                switcher.SelectedIndex -= 1;
                return;
            }

            if (e is { State: KeyState.Pressed, Key: Key.Right })
            {
                if (switcher.SelectedIndex + 1 >= switcher.GetNumSlots()) return;
                switcher.SelectedIndex += 1;
                return;
            }

            if (e is { State: KeyState.Pressed, Key: Key.Enter })
            {
                var p = Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true);
                foreach (var path in p)
                    switcher.AddChild(new Fitter(new AsyncFileImage(path))
                    {
                        FittingMode = FitMode.Cover
                    });
            }
        };
    }


    public void TestClip()
    {
        var surf = SWidgetsModule.Get().GetWindowSurface();

        if (surf == null) return;

        var panel = surf.Add(new Panel());

        var sizer = new Sizer(new Sizer(new Fitter(new AsyncFileImage(""))
        {
            FittingMode = FitMode.Cover
        })
        {
            WidthOverride = 600,
            HeightOverride = 600
        });

        panel.AddChild(new PanelSlot(sizer)
        {
            MinAnchor = 0.5f,
            MaxAnchor = 0.5f,
            Size = new Size2d(200, 200),
            Alignment = 0.5f
        });
    }
    
    public void TestText()
    {
        var surf = SWidgetsModule.Get().GetWindowSurface();

        if (surf == null) return;

        var panel = surf.Add(new Panel());

       
        var textSlot = panel.AddChild(new PanelSlot(new Text("Test Text", 40))
        {
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