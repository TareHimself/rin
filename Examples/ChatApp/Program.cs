// // See https://aka.ms/new-console-template for more information
//
// using Rin.Framework;
//
// SFramework.Get().Run();


using System.Numerics;
using Rin.Framework.Shared.Math;

var a = new Vector2(0.0f);


// var t = Matrix4x4.Identity.Translate(new Vector2(200,0)).Translate(new Vector2(0,100)).Rotate2dDegrees(90);
// var f = Matrix4x4.Identity.Translate(new Vector2(100,0)).Translate(new Vector2(0,200)).Rotate2dDegrees(90);
var t = Matrix4x4.Identity.Translate(new Vector2(200,0)).Rotate2dDegrees(20);
var f = Matrix4x4.Identity.Translate(new Vector2(100,0)).Rotate2dDegrees(20);

var b = a.Transform(t);
var c = a.Transform(f);

var x = a;