// using rin.Graphics.Material;
//
// namespace rin.Widgets.Graphics.Commands;
//
// public class MaterialRect : Command
// {
//     private readonly MaterialInstance _materialInstance;
//     private readonly WidgetPushConstants _pushConstants;
//
//     public MaterialRect(MaterialInstance materialInstance, WidgetPushConstants pushConstant)
//     {
//         materialInstance.Reserve();
//         _materialInstance = materialInstance;
//         _pushConstants = pushConstant;
//     }
//
//     protected override void OnDispose(bool isManual)
//     {
//         base.OnDispose(isManual);
//         _materialInstance.Dispose();
//     }
//     
//
//     protected override void Draw(WidgetFrame frame)
//     {
//         _materialInstance.BindTo(frame);
//         _materialInstance.BindBuffer("ui", frame.Surface.GlobalBuffer);
//         _materialInstance.Push(frame.Raw.GetCommandBuffer(),  _pushConstants);
//         Quad(frame);
//     }
// }