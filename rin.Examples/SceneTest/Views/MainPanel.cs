using System.Numerics;
using rin.Examples.SceneTest.entities;
using Rin.Editor.Scene;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Layouts;

namespace rin.Examples.SceneTest.Views;

public class MainPanel : Panel
{
    private Scene _scene = new Scene();
    private CameraActor _cameraActor;
    public MainPanel()
    {
        _cameraActor = _scene.AddActor<CameraActor>();
        var text = new TextBox();
        Slots =
        [
            new PanelSlot()
            {
                Child = new TestViewport(_cameraActor, text),
                MinAnchor = new Vector2(0.0f),
                MaxAnchor = new Vector2(1.0f),
            },
            new PanelSlot()
            {
                Child = text,
                SizeToContent = true
            }
        ];
    }
}