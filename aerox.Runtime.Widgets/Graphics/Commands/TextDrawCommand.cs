// using aerox.Runtime.Graphics.Material;
// using aerox.Runtime.Widgets.Content;
// using aerox.Runtime.Widgets.Mtsdf;
//
// namespace aerox.Runtime.Widgets.Graphics.Commands;
//
// public class TextDrawCommand : DrawCommand
// {
//     private readonly MtsdfFont _font;
//     private readonly MaterialInstance _materialInstance;
//     private readonly TextPushConstants[] _pushConstants;
//
//     public override bool CanCombineWith(Command other)
//     {
//         return other is TextDrawCommand drawCommand && drawCommand._font == _font;
//     }
//
//     public TextDrawCommand(MaterialInstance materialInstance, MtsdfFont font, TextPushConstants[] pushConstants)
//     {
//         materialInstance.Reserve();
//         font.Reserve();
//         _materialInstance = materialInstance;
//         _font = font;
//         _pushConstants = pushConstants;
//     }
//
//     protected override void OnDispose(bool isManual)
//     {
//         base.OnDispose(isManual);
//         _materialInstance.Dispose();
//         _font.Dispose();
//     }
//     
//
//     protected override void Draw(WidgetFrame frame)
//     {
//         _materialInstance.BindTo(frame);
//         _materialInstance.BindBuffer("ui", frame.Surface.GlobalBuffer);
//         foreach (var push in _pushConstants)
//         {
//             _materialInstance.Push(frame.Raw.GetCommandBuffer(),  push);
//             Quad(frame);
//         }
//     }
// }