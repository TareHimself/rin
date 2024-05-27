using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;

namespace aerox.Runtime.Scene.Graphics.Drawers.Deferred;

public class DeferredSceneDrawer : SceneDrawer
{
    protected MaterialInstance? _defaultMaterial;
    protected DeviceBuffer? GlobalBuffer;

    protected GBuffer? Images;

    public DeferredSceneDrawer(Scene scene) : base(scene)
    {
    }

    public override void Draw(Frame frame)
    {
        throw new NotImplementedException();
    }


    public override void Start()
    {
        base.Start();
        GlobalBuffer = SGraphicsModule.Get().GetAllocator()
            .NewUniformBuffer<SceneGlobalBuffer>(false, "Scene Global Buffer");

        //_defaultMaterial = 
    }
}