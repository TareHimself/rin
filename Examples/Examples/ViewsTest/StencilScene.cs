using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;

namespace Examples.ViewsTest;

public static class StencilScene
{
    public static void Create(IWindowRenderer renderer)
    {
        if (IViewsModule.Get().GetWindowSurface(renderer) is not { } surface) return;

        surface.Add(new PanelView
        {
            InitSlots =
            [
                Slot(new RectView { Color = new Color(0.45f, 0.47f, 0.52f, 1f) }, Vector2.Zero, new Vector2(900, 620)),
                Slot(SpinningClip(), new Vector2(160, 160), new Vector2(240, 240)),
                Slot(NestedClip(), new Vector2(340, 40), new Vector2(240, 240)),
                Slot(SpinningClip(true), new Vector2(740, 140), new Vector2(200, 200)),
                Slot(Rect(Color.White with { A = 0.9f }), new Vector2(120, 250), new Vector2(120, 120))
            ]
        });
    }

    private static PanelSlot Slot(IView child, Vector2 offset, Vector2 size)
    {
        return new PanelSlot { Child = child, Offset = offset, Size = size };
    }

    private static RectView Rect(Color color)
    {
        return new RectView { Color = color };
    }

    private static PanelView SpinningClip(bool counterSpin = false)
    {
        return new SpinningPanel(counterSpin ? -40f : 25f)
        {
            Clip = Clip.Bounds,
            InitSlots =
            [
                Slot(Rect(Color.Red), new Vector2(-150, -150), new Vector2(540, 540)),
                Slot(Rect(Color.Green), new Vector2(-10, -10), new Vector2(60, 60)),
                Slot(Rect(Color.Blue), new Vector2(150, 150), new Vector2(120, 120))
            ]
        };
    }

    private static PanelView NestedClip()
    {
        return new PanelView
        {
            Clip = Clip.Bounds,
            InitSlots =
            [
                Slot(Rect(new Color(0.25f, 0.25f, 0.3f, 1f)), Vector2.Zero, new Vector2(240, 240)),
                Slot(new SpinningPanel(-30f)
                {
                    Clip = Clip.Bounds,
                    InitSlots =
                    [
                        Slot(Rect(new Color(1f, 0.6f, 0.1f, 1f)), new Vector2(-100, -100), new Vector2(420, 420)),
                        Slot(Rect(Color.Black), new Vector2(60, 60), new Vector2(40, 40))
                    ]
                }, new Vector2(120, 120), new Vector2(200, 200))
            ]
        };
    }

    private sealed class SpinningPanel : PanelView
    {
        private readonly float _degreesPerSecond;

        public SpinningPanel(float degreesPerSecond)
        {
            _degreesPerSecond = degreesPerSecond;
            Pivot = new Vector2(0.5f);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            Angle += _degreesPerSecond * deltaTime;
        }
    }
}
