namespace rin.Framework.Core.Math;

/// <summary>
/// Set of easing functions generated using OpenAI GPT4
/// </summary>
public static class EasingFunctions
{
    public static double Linear(double alpha)
    {
        return alpha;
    }

    public static double EaseInQuad(double alpha)
    {
        return alpha * alpha;
    }

    public static double EaseOutQuad(double alpha)
    {
        return alpha * (2 - alpha);
    }

    public static double EaseInOutQuad(double alpha)
    {
        return alpha < 0.5 ? 2 * alpha * alpha : -1 + (4 - 2 * alpha) * alpha;
    }

    public static double EaseInCubic(double alpha)
    {
        return alpha * alpha * alpha;
    }

    public static double EaseOutCubic(double alpha)
    {
        return System.Math.Pow(alpha - 1, 3) + 1;
    }

    public static double EaseInOutCubic(double alpha)
    {
        return alpha < 0.5 ? 4 * alpha * alpha * alpha : (alpha - 1) * (2 * alpha - 2) * (2 * alpha - 2) + 1;
    }

    public static double EaseInQuart(double alpha)
    {
        return alpha * alpha * alpha * alpha;
    }

    public static double EaseOutQuart(double alpha)
    {
        return 1 - System.Math.Pow(alpha - 1, 4);
    }

    public static double EaseInOutQuart(double alpha)
    {
        return alpha < 0.5 ? 8 * alpha * alpha * alpha * alpha : 1 - 8 * System.Math.Pow(alpha - 1, 4);
    }

    public static double EaseInQuint(double alpha)
    {
        return alpha * alpha * alpha * alpha * alpha;
    }

    public static double EaseOutQuint(double alpha)
    {
        return System.Math.Pow(alpha - 1, 5) + 1;
    }

    public static double EaseInOutQuint(double alpha)
    {
        return alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 + 16 * System.Math.Pow(alpha - 1, 5);
    }

    public static double EaseInSine(double alpha)
    {
        return 1 - System.Math.Cos(alpha * System.Math.PI / 2);
    }

    public static double EaseOutSine(double alpha)
    {
        return System.Math.Sin(alpha * System.Math.PI / 2);
    }

    public static double EaseInOutSine(double alpha)
    {
        return 0.5 * (1 - System.Math.Cos(alpha * System.Math.PI));
    }

    public static double EaseInExpo(double alpha)
    {
        return alpha == 0 ? 0 : System.Math.Pow(2, 10 * (alpha - 1));
    }

    public static double EaseOutExpo(double alpha)
    {
        return System.Math.Abs(alpha - 1.0) < 0.001 ? 1 : 1 - System.Math.Pow(2, -10 * alpha);
    }

    public static double EaseInOutExpo(double alpha)
    {
        if (alpha == 0.0) return 0;
        if (System.Math.Abs(alpha - 1.0) < 0.001) return 1;
        return alpha < 0.5 ? 0.5 * System.Math.Pow(2, 20 * alpha - 10) : 1 - 0.5 * System.Math.Pow(2, -20 * alpha + 10);
    }

    public static double EaseInCirc(double alpha)
    {
        return 1 - System.Math.Sqrt(1 - alpha * alpha);
    }

    public static double EaseOutCirc(double alpha)
    {
        return System.Math.Sqrt(1 - System.Math.Pow(alpha - 1, 2));
    }

    public static double EaseInOutCirc(double alpha)
    {
        return alpha < 0.5
            ? (1 - System.Math.Sqrt(1 - 4 * alpha * alpha)) / 2
            : (System.Math.Sqrt(1 - System.Math.Pow(-2 * alpha + 2, 2)) + 1) / 2;
    }

    public static double EaseInBack(double alpha, double s = 1.70158)
    {
        return alpha * alpha * ((s + 1) * alpha - s);
    }

    public static double EaseOutBack(double alpha, double s = 1.70158)
    {
        return (alpha - 1) * (alpha - 1) * ((s + 1) * (alpha - 1) + s) + 1;
    }

    public static double EaseInOutBack(double alpha, double s = 1.70158)
    {
        s *= 1.525;
        return alpha < 0.5
            ? (System.Math.Pow(2 * alpha, 2) * ((s + 1) * 2 * alpha - s)) / 2
            : (System.Math.Pow(2 * alpha - 2, 2) * ((s + 1) * (alpha * 2 - 2) + s) + 2) / 2;
    }

    public static double EaseInBounce(double alpha)
    {
        return 1 - EaseOutBounce(1 - alpha);
    }

    public static double EaseOutBounce(double alpha)
    {
        if (alpha < 1 / 2.75)
        {
            return 7.5625 * alpha * alpha;
        }
        else if (alpha < 2 / 2.75)
        {
            return 7.5625 * (alpha -= 1.5 / 2.75) * alpha + 0.75;
        }
        else if (alpha < 2.5 / 2.75)
        {
            return 7.5625 * (alpha -= 2.25 / 2.75) * alpha + 0.9375;
        }
        else
        {
            return 7.5625 * (alpha -= 2.625 / 2.75) * alpha + 0.984375;
        }
    }

    public static double EaseInOutBounce(double alpha)
    {
        return alpha < 0.5
            ? (1 - EaseOutBounce(1 - 2 * alpha)) / 2
            : (1 + EaseOutBounce(2 * alpha - 1)) / 2;
    }
}