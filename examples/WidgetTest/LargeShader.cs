// using rin.Framework.Core;
// using rin.Framework.Graphics;
// using rin.Framework.Graphics.Material;
// using rin.Framework.Views;
// using rin.Framework.Views.Graphics;
//
// namespace WidgetTest;
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
//         _materialInstance = new MaterialInstance(Path.Join(SWidgetsModule.ShadersDir,"pretty.ash"));
//     }
//     
//     protected override void OnAddedToSurface(WidgetSurface widgetSurface)
//     {
//         base.OnAddedToSurface(widgetSurface);
//         _materialInstance.BindBuffer("ui", widgetSurface.GlobalBuffer);
//     }
//     protected override Vector2<float> ComputeDesiredSize() => new Vector2<float>(0, 0);
//
//     public override void Collect(WidgetFrame frame, TransformInfo info)
//     {
//         frame.AddMaterialRect(_materialInstance, new WidgetPushConstants()
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