using System.Numerics;
using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics.Quads;

public static class QuadExtensions
{
    public static PassCommands AddQuads(this PassCommands passCommands, params Quad[] quads) =>
        passCommands.Add(new QuadDrawCommand(quads));
    
    public static PassCommands AddRect(this PassCommands passCommands, Mat3 transform, Vector2 size,
        Vector4? color = null, Vector4? borderRadius = null) =>
        passCommands.AddQuads(Quad.Rect(transform, size,color,borderRadius));
    
    public static PassCommands AddTexture(this PassCommands passCommands, int textureId, Mat3 transform,
        Vector2 size, Vector4? tint = null, Vector4? uv = null,
        Vector4? borderRadius = null) =>
        passCommands.AddQuads(Quad.Texture(textureId,transform,size,tint,borderRadius,uv));
    
    
    public static PassCommands AddSdf(this PassCommands passCommands, int atlasId, Mat3 transform,
        Vector2 size, Vector4? color = null, Vector4? uv = null) =>
        passCommands.AddQuads(Quad.Sdf(atlasId,transform,size,color,uv));
}