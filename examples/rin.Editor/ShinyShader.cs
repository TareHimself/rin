using rin.Runtime.Core;
using rin.Graphics;
using rin.Graphics.Material;
using rin.Runtime.Views;
using rin.Runtime.Views.Graphics;

namespace rin.Editor;

public class ShinyShader : View
{
    private readonly MaterialInstance _materialInstance;

    public ShinyShader()
    {
        var gs = SRuntime.Get().GetModule<SGraphicsModule>();

       // _materialInstance = SViewsModule.CreateMaterial(Path.Join(SRuntime.ShadersDir,"pretty.ash"));
    }   

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _materialInstance.Dispose();
        ;
    }

    protected override void OnAddedToSurface(Surface surface)
    {
        base.OnAddedToSurface(surface);
        _materialInstance.BindBuffer("ui", surface.GlobalBuffer);
    }

    protected override void OnRemovedFromSurface(Surface surface)
    {
        base.OnRemovedFromSurface(surface);
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return new Vector2<float>(400, 400);
    }

    public override void Collect(ViewFrame frame, TransformInfo info)
    {
        frame.AddMaterialRect(_materialInstance, info);
    }
}