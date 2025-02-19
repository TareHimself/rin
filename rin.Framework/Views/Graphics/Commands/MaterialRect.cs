// using rin.Framework.Graphics.Material;
//
// namespace rin.Framework.Views.Graphics.Commands;
//
// public class MaterialRect : Command
// {
//     private readonly MaterialInstance _materialInstance;
//     private readonly ViewPushConstants _pushConstants;
//
//     public MaterialRect(MaterialInstance materialInstance, ViewPushConstants pushConstant)
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
//     protected override void Draw(ViewFrame frame)
//     {
//         _materialInstance.BindTo(frame);
//         _materialInstance.BindBuffer("ui", frame.Surface.GlobalBuffer);
//         _materialInstance.Push(frame.Raw.GetCommandBuffer(),  _pushConstants);
//         Quad(frame);
//     }
// }

