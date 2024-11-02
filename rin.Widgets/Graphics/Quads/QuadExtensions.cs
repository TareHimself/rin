using rin.Core.Math;

namespace rin.Widgets.Graphics.Quads;

public static class QuadExtensions
{
    public static DrawCommands AddQuads(this DrawCommands drawCommands, params Quad[] quads) =>
        drawCommands.Add(new QuadDrawCommand(quads));
    
    public static DrawCommands AddRect(this DrawCommands drawCommands,Vector2<float> size,Matrix3 transform,Vector4<float>? color = null,Vector4<float>? uv = null,Vector4<float>? borderRadius = null) =>
        drawCommands.AddQuads(new Quad(size,transform)
        {
            Color = color.GetValueOrDefault(Color.White),
            UV = uv.GetValueOrDefault(new Vector4<float>(0.0f,0.0f,1.0f,1.0f)),
            BorderRadius = borderRadius.GetValueOrDefault(0.0f)
        });
    
    public static DrawCommands AddTexture(this DrawCommands drawCommands, int textureId,Vector2<float> size,Matrix3 transform,Vector4<float>? color = null,Vector4<float>? uv = null,Vector4<float>? borderRadius = null) =>
        drawCommands.AddQuads(new Quad(size,transform,textureId)
        {
            Color = color.GetValueOrDefault(Color.White),
            UV = uv.GetValueOrDefault( new Vector4<float>(0.0f,0.0f,1.0f,1.0f)),
            BorderRadius = borderRadius.GetValueOrDefault(0.0f)
        });
    
    
    public static DrawCommands AddSdf(this DrawCommands drawCommands, int atlasId,Vector2<float> size,Matrix3 transform,Vector4<float>? color = null,Vector4<float>? uv = null,Vector4<float>? borderRadius = null) =>
        drawCommands.AddQuads(new Quad(size,transform,atlasId,1)
        {
            Color = color.GetValueOrDefault(Color.White),
            UV = uv.GetValueOrDefault( new Vector4<float>(0.0f,0.0f,1.0f,1.0f)),
            BorderRadius = borderRadius.GetValueOrDefault(0.0f)
        });
}