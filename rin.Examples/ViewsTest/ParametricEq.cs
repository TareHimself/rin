using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Audio.Effects;

namespace rin.Examples.ViewsTest;

[StructLayout(LayoutKind.Sequential)]
public struct BiquadState
{
    public float X1, X2;
    public float Y1, Y2;
}

[StructLayout(LayoutKind.Sequential)]
public struct EqParameters
{
    // Just dB gains per band — no sample rate needed
    public float G0, G1, G2, G3, G4, G5;

    public static EqParameters Flat          => new() { G0= 0, G1= 0, G2= 0, G3= 0, G4= 0, G5= 0 };
    public static EqParameters Acoustic      => new() { G0= 4, G1= 3, G2= 2, G3= 3, G4= 4, G5= 5 };
    public static EqParameters BassBooster   => new() { G0= 8, G1= 6, G2= 4, G3= 0, G4= 0, G5= 0 };
    public static EqParameters BassReducer   => new() { G0=-8, G1=-6, G2=-4, G3= 0, G4= 0, G5= 0 };
    public static EqParameters Classical     => new() { G0= 4, G1= 3, G2= 0, G3= 0, G4= 2, G5= 4 };
    public static EqParameters Dance         => new() { G0= 6, G1= 4, G2= 1, G3=-1, G4= 3, G5= 5 };
    public static EqParameters Deep          => new() { G0= 5, G1= 4, G2= 4, G3= 2, G4= 0, G5=-2 };
    public static EqParameters Electronic    => new() { G0= 5, G1= 3, G2=-1, G3= 2, G4= 3, G5= 5 };
    public static EqParameters HipHop        => new() { G0= 7, G1= 5, G2=-1, G3=-2, G4= 3, G5= 5 };
    public static EqParameters Jazz          => new() { G0= 3, G1= 2, G2= 1, G3= 2, G4= 3, G5= 4 };
    public static EqParameters Latin         => new() { G0= 4, G1= 2, G2= 0, G3= 0, G4= 3, G5= 5 };
    public static EqParameters Loudness      => new() { G0= 6, G1= 3, G2= 0, G3= 0, G4= 3, G5= 6 };
    public static EqParameters Lounge        => new() { G0=-3, G1=-1, G2= 2, G3= 3, G4= 2, G5=-2 };
    public static EqParameters Piano         => new() { G0= 0, G1= 2, G2= 4, G3= 4, G4= 5, G5= 5 };
    public static EqParameters Pop           => new() { G0=-1, G1= 3, G2= 5, G3= 4, G4= 2, G5=-1 };
    public static EqParameters RnB           => new() { G0= 7, G1= 5, G2=-1, G3=-2, G4= 3, G5= 5 };
    public static EqParameters Rock          => new() { G0= 6, G1= 4, G2=-1, G3=-2, G4= 3, G5= 6 };
    public static EqParameters SmallSpeakers => new() { G0= 4, G1= 3, G2= 2, G3= 0, G4=-2, G5=-4 };
    public static EqParameters SpokenWord    => new() { G0=-3, G1=-1, G2= 3, G3= 5, G4= 4, G5= 2 };
    public static EqParameters TrebleBooster => new() { G0= 0, G1= 0, G2= 0, G3= 3, G4= 5, G5= 8 };
    public static EqParameters TrebleReducer => new() { G0= 0, G1= 0, G2= 0, G3=-3, G4=-5, G5=-8 };
    public static EqParameters VocalBooster  => new() { G0=-2, G1= 0, G2= 4, G3= 5, G4= 4, G5= 0 };
}

[StructLayout(LayoutKind.Sequential)]
public struct EqState
{
    // Cached biquad coefficients (recomputed when SR or gains change)
    public EqCoeffCache Coeffs;

    // Delay lines
    public BiquadState Band0, Band1, Band2, Band3, Band4, Band5;

    // Last seen values — used to detect when to recompute
    public float CachedSampleRate;
    public float CG0, CG1, CG2, CG3, CG4, CG5;
}

[StructLayout(LayoutKind.Sequential)]
public struct EqCoeffCache
{
    public float B0_0, B1_0, B2_0, A1_0, A2_0;
    public float B0_1, B1_1, B2_1, A1_1, A2_1;
    public float B0_2, B1_2, B2_2, A1_2, A2_2;
    public float B0_3, B1_3, B2_3, A1_3, A2_3;
    public float B0_4, B1_4, B2_4, A1_4, A2_4;
    public float B0_5, B1_5, B2_5, A1_5, A2_5;
}

public static class EqCoefficients
{
    public static void Compute(ref EqCoeffCache c, float sr, in EqParameters p)
    {
        SetShelf(ref c, 0, sr,    60f, p.G0, high: false);
        SetPeak (ref c, 1, sr,   150f, p.G1, q: 0.8f);
        SetPeak (ref c, 2, sr,   400f, p.G2, q: 0.8f);
        SetPeak (ref c, 3, sr,  1000f, p.G3, q: 0.8f);
        SetPeak (ref c, 4, sr,  2400f, p.G4, q: 0.8f);
        SetShelf(ref c, 5, sr, 15000f, p.G5, high: true);
    }

    static void SetPeak(ref EqCoeffCache c, int band, float fs, float f0, float dBgain, float q)
    {
        double A     = Math.Pow(10.0, dBgain / 40.0);
        double w0    = 2.0 * Math.PI * f0 / fs;
        double cw    = Math.Cos(w0), sw = Math.Sin(w0);
        double alpha = sw / (2.0 * q);
        Write(ref c, band,
            b0: 1 + alpha * A,  b1: -2 * cw,  b2: 1 - alpha * A,
            a0: 1 + alpha / A,  a1: -2 * cw,  a2: 1 - alpha / A);
    }

    static void SetShelf(ref EqCoeffCache c, int band, float fs, float f0, float dBgain, bool high)
    {
        var a    = Math.Pow(10.0, dBgain / 40.0);
        var w0   = 2.0 * Math.PI * f0 / fs;
        double cw   = Math.Cos(w0), sw = Math.Sin(w0);
        var alpha = sw / 2.0 * Math.Sqrt((a + 1.0 / a) * (1.0 / 1.0 - 1.0) + 2.0);
        var sqA2 = 2.0 * Math.Sqrt(a) * alpha;
        double b0, b1, b2, a0, a1, a2;
        if (!high)
        {
            b0 =      a*((a+1)-(a-1)*cw+sqA2); b1= 2*a*((a-1)-(a+1)*cw); b2=      a*((a+1)-(a-1)*cw-sqA2);
            a0 =         (a+1)+(a-1)*cw+sqA2;  a1=  -2*((a-1)+(a+1)*cw); a2=         (a+1)+(a-1)*cw-sqA2;
        }
        else
        {
            b0 =      a*((a+1)+(a-1)*cw+sqA2); b1=-2*a*((a-1)+(a+1)*cw); b2=      a*((a+1)+(a-1)*cw-sqA2);
            a0 =         (a+1)-(a-1)*cw+sqA2;  a1=   2*((a-1)-(a+1)*cw); a2=         (a+1)-(a-1)*cw-sqA2;
        }
        Write(ref c, band, b0, b1, b2, a0, a1, a2);
    }

    static void Write(ref EqCoeffCache c, int band,
        double b0, double b1, double b2, double a0, double a1, double a2)
    {
        float nb0=(float)(b0/a0), nb1=(float)(b1/a0), nb2=(float)(b2/a0),
              na1=(float)(a1/a0), na2=(float)(a2/a0);
        switch (band)
        {
            case 0: c.B0_0=nb0;c.B1_0=nb1;c.B2_0=nb2;c.A1_0=na1;c.A2_0=na2; break;
            case 1: c.B0_1=nb0;c.B1_1=nb1;c.B2_1=nb2;c.A1_1=na1;c.A2_1=na2; break;
            case 2: c.B0_2=nb0;c.B1_2=nb1;c.B2_2=nb2;c.A1_2=na1;c.A2_2=na2; break;
            case 3: c.B0_3=nb0;c.B1_3=nb1;c.B2_3=nb2;c.A1_3=na1;c.A2_3=na2; break;
            case 4: c.B0_4=nb0;c.B1_4=nb1;c.B2_4=nb2;c.A1_4=na1;c.A2_4=na2; break;
            case 5: c.B0_5=nb0;c.B1_5=nb1;c.B2_5=nb2;c.A1_5=na1;c.A2_5=na2; break;
        }
    }
}

[AudioEffect]
public partial struct ParametricEq
{
    public static void Process(
        ReadOnlySpan<float> input,
        Span<float> output,
        AudioEffectContext ctx,
        ref EqState state,
        in EqParameters parameters)
    {
        
        float sr = ctx.SampleRate;

        // Recompute coefficients only when SR or any gain changes
        if (sr != state.CachedSampleRate ||
            parameters.G0 != state.CG0 || parameters.G1 != state.CG1 || parameters.G2 != state.CG2 ||
            parameters.G3 != state.CG3 || parameters.G4 != state.CG4 || parameters.G5 != state.CG5)
        {
            EqCoefficients.Compute(ref state.Coeffs, sr, parameters);
            state.CachedSampleRate = sr;
            state.CG0 = parameters.G0; state.CG1 = parameters.G1; state.CG2 = parameters.G2;
            state.CG3 = parameters.G3; state.CG4 = parameters.G4; state.CG5 = parameters.G5;
        }

        ref readonly var c = ref state.Coeffs;
        for (var i = 0; i < input.Length; i++)
        {
            var s = input[i];
            s = Biquad(s, ref state.Band0, c.B0_0, c.B1_0, c.B2_0, c.A1_0, c.A2_0);
            s = Biquad(s, ref state.Band1, c.B0_1, c.B1_1, c.B2_1, c.A1_1, c.A2_1);
            s = Biquad(s, ref state.Band2, c.B0_2, c.B1_2, c.B2_2, c.A1_2, c.A2_2);
            s = Biquad(s, ref state.Band3, c.B0_3, c.B1_3, c.B2_3, c.A1_3, c.A2_3);
            s = Biquad(s, ref state.Band4, c.B0_4, c.B1_4, c.B2_4, c.A1_4, c.A2_4);
            s = Biquad(s, ref state.Band5, c.B0_5, c.B1_5, c.B2_5, c.A1_5, c.A2_5);
            output[i] = s;
        }
        
    }

    private static float Biquad(float x, ref BiquadState s,
        float b0, float b1, float b2, float a1, float a2)
    {
        float y = b0 * x + s.X1;
        s.X1 = b1 * x - a1 * y + s.X2;
        s.X2 = b2 * x - a2 * y;
        return y;
    }
}