namespace Rin.Framework.Shared.Math;

/// <summary>
///     Set of easing functions generated using OpenAI GPT4
/// </summary>
public static class EasingFunctions
{
    public static float Linear(float alpha)
    {
        return alpha;
    }

    public static float EaseInQuad(float alpha)
    {
        return alpha * alpha;
    }

    public static float EaseOutQuad(float alpha)
    {
        return alpha * (2 - alpha);
    }

    public static float EaseInOutQuad(float alpha)
    {
        return alpha < 0.5 ? 2 * alpha * alpha : -1 + (4 - 2 * alpha) * alpha;
    }

    public static float EaseInCubic(float alpha)
    {
        return alpha * alpha * alpha;
    }

    public static float EaseOutCubic(float alpha)
    {
        return float.Pow(alpha - 1, 3) + 1;
    }

    public static float EaseInOutCubic(float alpha)
    {
        return alpha < 0.5 ? 4 * alpha * alpha * alpha : (alpha - 1) * (2 * alpha - 2) * (2 * alpha - 2) + 1;
    }

    public static float EaseInQuart(float alpha)
    {
        return alpha * alpha * alpha * alpha;
    }

    public static float EaseOutQuart(float alpha)
    {
        return 1 - float.Pow(alpha - 1, 4);
    }

    public static float EaseInOutQuart(float alpha)
    {
        return alpha < 0.5f ? 8 * alpha * alpha * alpha * alpha : 1 - 8 * float.Pow(alpha - 1, 4);
    }

    public static float EaseInQuint(float alpha)
    {
        return alpha * alpha * alpha * alpha * alpha;
    }

    public static float EaseOutQuint(float alpha)
    {
        return float.Pow(alpha - 1, 5) + 1;
    }

    public static float EaseInOutQuint(float alpha)
    {
        return alpha < 0.5
            ? 16 * alpha * alpha * alpha * alpha * alpha
            : 1 + 16 * float.Pow(alpha - 1, 5);
    }

    public static float EaseInSine(float alpha)
    {
        return 1 - float.Cos(alpha * float.Pi / 2);
    }

    public static float EaseOutSine(float alpha)
    {
        return float.Sin(alpha * float.Pi / 2);
    }

    public static float EaseInOutSine(float alpha)
    {
        return 0.5f * (1 - float.Cos(alpha * float.Pi));
    }

    public static float EaseInExpo(float alpha)
    {
        return alpha == 0 ? 0 : float.Pow(2, 10 * (alpha - 1));
    }

    public static float EaseOutExpo(float alpha)
    {
        return float.Abs(alpha - 1.0f) < 0.001 ? 1 : 1 - float.Pow(2, -10 * alpha);
    }

    public static float EaseInOutExpo(float alpha)
    {
        if (alpha == 0.0) return 0;
        if (float.Abs(alpha - 1.0f) < 0.001f) return 1;
        return alpha < 0.5f
            ? 0.5f * float.Pow(2, 20 * alpha - 10)
            : 1 - 0.5f * float.Pow(2, -20 * alpha + 10);
    }

    public static float EaseInCirc(float alpha)
    {
        return 1 - float.Sqrt(1 - alpha * alpha);
    }

    public static float EaseOutCirc(float alpha)
    {
        return float.Sqrt(1 - float.Pow(alpha - 1, 2));
    }

    public static float EaseInOutCirc(float alpha)
    {
        return alpha < 0.5
            ? (1 - float.Sqrt(1 - 4 * alpha * alpha)) / 2
            : (float.Sqrt(1 - float.Pow(-2 * alpha + 2, 2)) + 1) / 2;
    }

    public static float EaseInBack(float alpha, float s = 1.70158f)
    {
        return alpha * alpha * ((s + 1) * alpha - s);
    }

    public static float EaseOutBack(float alpha, float s = 1.70158f)
    {
        return (alpha - 1) * (alpha - 1) * ((s + 1) * (alpha - 1) + s) + 1;
    }

    public static float EaseInOutBack(float alpha, float s = 1.70158f)
    {
        s *= 1.525f;
        return alpha < 0.5f
            ? float.Pow(2 * alpha, 2) * ((s + 1) * 2 * alpha - s) / 2
            : (float.Pow(2 * alpha - 2, 2) * ((s + 1) * (alpha * 2 - 2) + s) + 2) / 2;
    }

    public static float EaseInBounce(float alpha)
    {
        return 1 - EaseOutBounce(1 - alpha);
    }

    public static float EaseOutBounce(float alpha)
    {
        if (alpha < 1 / 2.75)
            return 7.5625f * alpha * alpha;
        if (alpha < 2 / 2.75)
            return 7.5625f * (alpha -= 1.5f / 2.75f) * alpha + 0.75f;
        if (alpha < 2.5 / 2.75)
            return 7.5625f * (alpha -= 2.25f / 2.75f) * alpha + 0.9375f;
        return 7.5625f * (alpha -= 2.625f / 2.75f) * alpha + 0.984375f;
    }

    public static float EaseInOutBounce(float alpha)
    {
        return alpha < 0.5
            ? (1 - EaseInBounce(1 - 2 * alpha)) / 2
            : (1 + EaseOutBounce(2 * alpha - 1)) / 2;
    }
}