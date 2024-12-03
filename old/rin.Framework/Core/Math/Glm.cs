using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;

public static partial class Glm
{
    
    


    public static Matrix4 Orthographic(float left, float right, float bottom, float top)
    {
        var result = Matrix4.Identity;
        NativeMethods.NativeGlmOrthographic(ref result, left, right, bottom, top);
        return result;
    }
    

    public static Matrix4 Perspective(float fov,float aspect,float near,float far)
    {
        var result = Matrix4.Identity;
        NativeMethods.NativeGlmPerspective(ref result,fov,aspect,near,far);
        return result;
    }
}