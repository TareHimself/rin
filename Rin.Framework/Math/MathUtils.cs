using System.Numerics;

namespace Rin.Framework.Math;

public static class MathUtils
{
    public static float InterpolateTo(float current, float target, float deltaTime, float speed)
    {
        var dist = target - current;
        var delta = speed * deltaTime;
        if (System.Math.Abs(delta) > System.Math.Abs(dist)) return target;
        if (dist < 0) delta *= -1.0f;

        return current + delta;
    }

    public static Vector2 InterpolateTo(Vector2 begin, Vector2 end, float deltaTime, float speed)
    {
        return new Vector2(InterpolateTo(begin.X, end.X, deltaTime, speed),
            InterpolateTo(begin.Y, end.Y, deltaTime, speed));
    }

    /// <summary>
    ///     Interpolates from <see cref="begin" /> to <see cref="end" /> using <see cref="alpha" />. The default
    ///     <see cref="method" /> is <a href="https://en.wikipedia.org/wiki/Linear_interpolation">Linear</a>
    /// </summary>
    /// <param name="begin">The start</param>
    /// <param name="end">The end</param>
    /// <param name="alpha">The alpha</param>
    /// <param name="method">An optional method for interpolation</param>
    /// <returns></returns>
    public static float Interpolate(float begin, float end, float alpha,
        Func<float, float, float, float>? method = null)
    {
        if (method == null) return float.Clamp(begin + (end - begin) * alpha, begin, end);

        return float.Clamp(method(begin, end, alpha), begin, end);
    }

    /// <summary>
    ///     Interpolates from <see cref="begin" /> to <see cref="end" /> using <see cref="alpha" />. The default
    ///     <see cref="method" /> is <a href="https://en.wikipedia.org/wiki/Linear_interpolation">Linear</a>
    /// </summary>
    /// <param name="begin">The start</param>
    /// <param name="end">The end</param>
    /// <param name="alpha">The alpha</param>
    /// <param name="method">An optional method for interpolation</param>
    /// <returns></returns>
    public static Vector2 Interpolate(Vector2 begin, Vector2 end, float alpha,
        Func<float, float, float, float>? method = null)
    {
        return new Vector2(Interpolate(begin.X, end.X, alpha, method),
            Interpolate(begin.Y, end.Y, alpha, method));
    }

    /// <summary>
    ///     Interpolates from <see cref="begin" /> to <see cref="end" /> using <see cref="alpha" />. The default
    ///     <see cref="method" /> is <a href="https://en.wikipedia.org/wiki/Linear_interpolation">Linear</a>
    /// </summary>
    /// <param name="begin">The start</param>
    /// <param name="end">The end</param>
    /// <param name="alpha">The alpha</param>
    /// <param name="method">An optional method for interpolation</param>
    /// <returns></returns>
    public static Vector3 Interpolate(Vector3 begin, Vector3 end, float alpha,
        Func<float, float, float, float>? method = null)
    {
        return new Vector3(Interpolate(begin.X, end.X, alpha, method),
            Interpolate(begin.Y, end.Y, alpha, method), Interpolate(begin.Z, end.Z, alpha, method));
    }

    /// <summary>
    ///     Interpolates from <see cref="begin" /> to <see cref="end" /> using <see cref="alpha" />. The default
    ///     <see cref="method" /> is <a href="https://en.wikipedia.org/wiki/Linear_interpolation">Linear</a>
    /// </summary>
    /// <param name="begin">The start</param>
    /// <param name="end">The end</param>
    /// <param name="alpha">The alpha</param>
    /// <param name="method">An optional method for interpolation</param>
    /// <returns></returns>
    public static Vector4 Interpolate(Vector4 begin, Vector4 end, float alpha,
        Func<float, float, float, float>? method = null)
    {
        return new Vector4(Interpolate(begin.X, end.X, alpha, method),
            Interpolate(begin.Y, end.Y, alpha, method), Interpolate(begin.Z, end.Z, alpha, method),
            Interpolate(begin.W, end.W, alpha, method));
    }

    /// <summary>
    ///     Interpolates from <see cref="begin" /> to <see cref="end" /> using <see cref="alpha" />. The default
    ///     <see cref="method" /> is <a href="https://en.wikipedia.org/wiki/Linear_interpolation">Linear</a>
    /// </summary>
    /// <param name="begin">The start</param>
    /// <param name="end">The end</param>
    /// <param name="alpha">The alpha</param>
    /// <param name="method">An optional method for interpolation</param>
    /// <returns></returns>
    public static Quaternion Interpolate(Quaternion begin, Quaternion end, float alpha,
        Func<float, float, float, float>? method = null)
    {
        return new Quaternion(Interpolate(begin.X, end.X, alpha, method), Interpolate(begin.Y, end.Y, alpha, method),
            Interpolate(begin.Z, end.Z, alpha, method), Interpolate(begin.W, end.W, alpha, method));
    }
}