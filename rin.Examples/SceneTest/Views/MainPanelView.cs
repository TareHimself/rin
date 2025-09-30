using System.Numerics;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Layouts;
using Rin.Engine.World;
using rin.Examples.SceneTest.entities;

namespace rin.Examples.SceneTest.Views;

public class MainPanelView : PanelView
{
    private readonly CameraActor _cameraActor;
    private readonly World _world = new();

    public MainPanelView()
    {
        _cameraActor = _world.AddActor<CameraActor>();
        Slots =
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