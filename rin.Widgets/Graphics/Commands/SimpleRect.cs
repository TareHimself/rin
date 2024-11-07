﻿// using rin.Core.Math;
//
// namespace rin.Widgets.Graphics.Commands;
//
// public class SimpleRect(Matrix3 transform, Vector2<float> size) : Command
// {
//     public Vector4<float>? BorderRadius;
//     public Color? Color;
//     
//     protected override void Draw(WidgetFrame frame)
//     {
//         frame.Surface.SimpleRectMat.BindTo(frame);
//         var constants = new SimpleRectPush
//         {
//             Transform = transform,
//             Size = size,
//             BorderRadius = BorderRadius ?? new Vector4<float>(1.0f),
//             Color = Color ?? Color.White
//         };
//         frame.Surface.SimpleRectMat.Push(frame.Raw.GetCommandBuffer(),  constants);
//         Quad(frame);
//     }
// }