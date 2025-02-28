﻿// using Rin.Engine.Graphics.Material;
// using Rin.Engine.Views.Content;
// using Rin.Engine.Views.Mtsdf;
//
// namespace Rin.Engine.Views.Graphics.Commands;
//
// public class TextDrawCommand : Command
// {
//     private readonly MtsdfFont _font;
//     private readonly MaterialInstance _materialInstance;
//     private readonly TextPushConstants[] _pushConstants;
//
//     public override bool CanCombineWith(GraphicsCommand other)
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
//     protected override void Draw(ViewFrame frame)
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

