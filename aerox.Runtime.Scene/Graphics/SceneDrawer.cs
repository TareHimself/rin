using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Scene.Graphics;

public abstract class SceneDrawer : Disposable, IDrawable, ILifeCycle
{
    public Scene OwningScene;

    public SceneDrawer(Scene scene)
    {
        OwningScene = scene;
    }

    public abstract void Draw(Frame frame);

    public void Tick(double deltaSeconds)
    {
    }

    public event Action<SceneFrame, Matrix4> OnDraw;

    public virtual void Start()
    {
    }

    protected override void OnDispose(bool isManual)
    {
    }

    public abstract VkFormat[] GetAttachmentFormats();

    public virtual MaterialBuilder SceneMaterialBuilder()
    {
        return new MaterialBuilder();
    }
}