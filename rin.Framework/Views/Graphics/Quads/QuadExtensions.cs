using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics.Quads;

public static class QuadExtensions
{
    public static DrawCommands AddQuads(this DrawCommands drawCommands, params Quad[] quads) =>
        drawCommands.Add(new QuadDrawCommand(quads));
    
    public static DrawCommands AddRect(this DrawCommands drawCommands, Mat3 transform, Vec2<float> size,
        Vec4<float>? color = null, Vec4<float>? borderRadius = null) =>
        drawCommands.AddQuads(Quad.Rect(transform, size,color,borderRadius));
    
    public static DrawCommands AddTexture(this DrawCommands drawCommands, int textureId, Mat3 transform,
        Vec2<float> size, Vec4<float>? tint = null, Vec4<float>? uv = null,
        Vec4<float>? borderRadius = null) =>
        drawCommands.AddQuads(Quad.Texture(textureId,transform,size,tint,borderRadius,uv));
    
    
    public static DrawCommands AddSdf(this DrawCommands drawCommands, int atlasId, Mat3 transform,
        Vec2<float> size, Vec4<float>? color = null, Vec4<float>? uv = null) =>
        drawCommands.AddQuads(Quad.Sdf(atlasId,transform,size,color,uv));
}