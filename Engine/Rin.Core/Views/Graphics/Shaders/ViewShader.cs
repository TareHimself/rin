using System.Numerics;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Shaders;

[ShadeExport]
public abstract class ViewShader<TFragmentIn> : Shader
{
    protected override BlendState BlendState => BlendState.Alpha;

    [Fragment, Attachment(AttachmentFormat.RGBA16), Stencil]
    public Vector4 Fragment(TFragmentIn input)
    {
        return Color(input);
    }

    protected abstract Vector4 Color(TFragmentIn input);
}
