using System.Numerics;
using Rin.World;
using Rin.World.Graphics.Default;
using Rin.World.Physics.Bepu;
using SceneTest.entities;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;

namespace SceneTest.Views;

public class MainPanelView : PanelView
{
    private readonly CameraActor _cameraActor;
    private readonly World _world = new(new DefaultRenderSystem(), new BepuPhysicsSystem());

    public MainPanelView()
    {
        _cameraActor = _world.AddActor<CameraActor>();
        InitSlots =
        [
            new PanelSlot
            {
                Child = new TestViewport(_cameraActor),
                MinAnchor = new Vector2(0.0f),
                MaxAnchor = new Vector2(1.0f)
            }
        ];
    }
}