// using Rin.Engine.Core;
// using Rin.Engine.Graphics;
// using Rin.Engine.Graphics.Material;
// using Rin.Engine.Views;
// using Rin.Engine.Views.Graphics;
//
// namespace ViewsTest;
//
// public class LargeShader : View
// {
//     
//     private readonly MaterialInstance _materialInstance;
//     
//     public LargeShader()
//     {
//         var gs = SRuntime.Get().GetModule<SGraphicsModule>();
//
//         _materialInstance = new MaterialInstance(Path.Join(SViewsModule.ShadersDir,"pretty.ash"));
//     }
//     
//     protected override void OnAddedToSurface(ViewSurface viewSurface)
//     {
//         base.OnAddedToSurface(viewSurface);
//         _materialInstance.BindBuffer("ui", viewSurface.GlobalBuffer);
//     }
//     protected override Vector2<float> ComputeDesiredSize() => new Vector2<float>(0, 0);
//
//     public override void Collect(ViewFrame frame, TransformInfo info)
//     {
//         frame.AddMaterialRect(_materialInstance, new ViewPushConstants()
//         {
//             Transform = this.ComputeRelativeTransform(),
//             Size = this.GetContentSize(),
//         });
//     }
//
//     protected override void OnDispose(bool isManual)
//     {
//         base.OnDispose(isManual);
//         _materialInstance.Dispose();
//     }
// }