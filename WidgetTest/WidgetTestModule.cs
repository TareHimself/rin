using aerox.Runtime;
using aerox.Runtime.Extensions;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Defaults.Containers;
using aerox.Runtime.Widgets.Defaults.Content;
using aerox.Runtime.Windows;

namespace WidgetTest;

[RuntimeModule(typeof(WidgetsModule))]
public class WidgetTestModule : RuntimeModule
{

    public void TestBlur()
    {
        var surf = WidgetsModule.Get().GetWindowSurface();
        
        if (surf == null) return;
        
        var panel = surf.Add(new Panel());

        var switcher = new Switcher();
        var imageSlot = panel.AddChild(
            switcher
        );

        panel.AddChild(new BackgroundBlur()
        {
            Tint = new Color(1.0f)
            {
                R = 0.3f,
                G = 0.3f,
                B = 0.3f,
            }
        })?.Mutate((slot) =>
        {
            slot.MinAnchor = 0.0f;
            slot.MaxAnchor = 1.0f;
        });

        var textSlot = panel.AddChild(new Text("Background Blur Using Blit", 150));

        if (textSlot == null || imageSlot == null) return;
        imageSlot.Mutate((slot) =>
        {
            slot.MaxAnchor = 1.0f;
        });
        textSlot.Mutate((slot) =>
        {
            slot.SizeToContent = true;
            slot.Alignment = 0.5f;
            slot.MinAnchor = 0.5f;
            slot.MaxAnchor = 0.5f;
        });
        
        int frames = 0;
        Runtime.Instance.OnTick += (d) =>
        {
            frames++;
            var txt = (Text)textSlot.GetWidget();
            txt.Content = $"{(int)Runtime.Instance.GetTimeSinceCreation()}";
        };

        surf.Window.OnKey += (e) =>
        {
            if (e is { State: EKeyState.Pressed, Key: EKey.KeyLeft })
            {
                if(switcher.SelectedIndex - 1 < 0) return;
                switcher.SelectedIndex -= 1;
                return;
            }

            if (e is { State: EKeyState.Pressed, Key: EKey.KeyRight })
            {
                if(switcher.SelectedIndex + 1 >= switcher.GetNumSlots()) return;
                switcher.SelectedIndex += 1;
                return;
            }

            if (e is { State: EKeyState.Pressed, Key: EKey.KeyEnter })
            {
                Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true).Then((p) =>
                {
                    foreach (var path in p)
                    {
                        switcher.AddChild(new Fitter(new AsyncFileImage(path))
                        {
                            FittingMode = FitMode.Cover
                        });
                    }
                });
                return;
            }
        };
    }


    public void TestClip()
    {
        var surf = WidgetsModule.Get().GetWindowSurface();
        
        if (surf == null) return;
        
        var panel = surf.Add(new Panel());

        var sizer = new Sizer(new Sizer(new Fitter(new AsyncFileImage())
        {
          FittingMode  = FitMode.Cover
        })
        {
            WidthOverride = 600,
            HeightOverride = 600
        });
        
        panel.AddChild(sizer)?.Mutate((slot) =>
        {
            slot.MinAnchor = 0.5f;
            slot.MaxAnchor = 0.5f;
            slot.Size = new Size2d(200, 200);
            slot.Alignment = 0.5f;
        });
    }
    public override void Startup(Runtime runtime)
    {
        base.Startup(runtime);
        var window = WindowsModule.Get().CreateWindow(500, 500, "Aerox Widget Test");
        
        

       TestBlur();
    }
}