using rin.Examples.SceneTest.entities;
using rin.Framework.Scene;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Content;
using rin.Framework.Views.Layouts;

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
                MinAnchor = 0.0f,
                MaxAnchor = 1.0f,
            },
            new PanelSlot()
            {
                Child = text,
                SizeToContent = true
            }
        ];
    }
}