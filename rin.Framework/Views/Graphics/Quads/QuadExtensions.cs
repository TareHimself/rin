using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics.Quads;

public static class QuadExtensions
{
    public static DrawCommands AddQuads(this DrawCommands drawCommands, params Quad[] quads) =>
        drawCommands.Add(new QuadDrawCommand(quads));
    
    public static DrawCommands AddRect(this DrawCommands drawCommands, Matrix3 transform, Vector2<float> size,
        Vector4<float>? color = null, Vector4<float>? borderRadius = null) =>
        drawCommands.AddQuads(Quad.NewRect(transform, size,color,borderRadius));
    
    public static DrawCommands AddTexture(this DrawCommands drawCommands, int textureId, Matrix3 transform,
        Vector2<float> size, Vector4<float>? tint = null, Vector4<float>? uv = null,
        Vector4<float>? borderRadius = null) =>
        drawCommands.AddQuads(Quad.NewTexture(textureId,transform,size,tint,borderRadius,uv));
    
    
    public static DrawCommands AddSdf(this DrawCommands drawCommands, int atlasId, Matrix3 transform,
        Vector2<float> size, Vector4<float>? color = null, Vector4<float>? uv = null) =>
        drawCommands.AddQuads(Quad.NewSdf(atlasId,transform,size,color,uv));
}