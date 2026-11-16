#!/usr/bin/env python3
"""Generate Shade/Rin.Shade/Shader.Math.cs: [SlangExpression]-bound static methods on the Shader base
class for Slang's common math builtins, callable unqualified from a shader (Lerp(a, b, t)), one
explicit overload per relevant width (float, Vector2/3/4) -
Rin.Shade doesn't support generic methods (SHADE0008), so each width needs its own overload
rather than a single generic declaration.

Each method also has a C# body that computes what the Slang builtin does, so shader code that calls
it runs unchanged as ordinary C#, in a unit test or on the CPU. The transpiler never reads the body:
it replaces the call with the attribute's Slang expression.

Run from anywhere; writes directly to Shade/Rin.Shade/Shader.Math.cs relative to the repo root.
"""

from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
OUTPUT_PATH = REPO_ROOT / "Shade" / "Rin.Shade" / "Shader.Math.cs"

ALL_WIDTHS = [1, 2, 3, 4]  # 1 = float
VECTOR_WIDTHS = [2, 3, 4]
COMPONENTS = ["X", "Y", "Z", "W"]

# What each per-component builtin does to floats, written over the parameter names declared below.
SCALAR_BODIES = {
    "Abs": "MathF.Abs(x)",
    # MathF.Sign throws on NaN, where the shader builtin returns 0.
    "Sign": "x > 0f ? 1f : x < 0f ? -1f : 0f",
    "Floor": "MathF.Floor(x)",
    "Ceil": "MathF.Ceiling(x)",
    "Round": "MathF.Round(x)",
    "Frac": "x - MathF.Floor(x)",
    "Sqrt": "MathF.Sqrt(x)",
    "Rsqrt": "1f / MathF.Sqrt(x)",
    "Saturate": "MathF.Min(MathF.Max(x, 0f), 1f)",
    "Exp": "MathF.Exp(x)",
    "Exp2": "MathF.Pow(2f, x)",
    "Log": "MathF.Log(x)",
    "Log2": "MathF.Log2(x)",
    "Sin": "MathF.Sin(x)",
    "Cos": "MathF.Cos(x)",
    "Tan": "MathF.Tan(x)",
    "Asin": "MathF.Asin(x)",
    "Acos": "MathF.Acos(x)",
    "Atan": "MathF.Atan(x)",
    "Radians": "x * (MathF.PI / 180f)",
    "Degrees": "x * (180f / MathF.PI)",
    "Min": "MathF.Min(a, b)",
    "Max": "MathF.Max(a, b)",
    "Pow": "MathF.Pow(a, b)",
    "Step": "b >= a ? 1f : 0f",
    "Atan2": "MathF.Atan2(a, b)",
    # Not Math.Clamp, which throws when lo is above hi.
    "Clamp": "MathF.Min(MathF.Max(x, lo), hi)",
    "SmoothStep": "Hermite(Saturate((x - edge0) / (edge1 - edge0)))",
}

# Whole-vector builtins, written with {V} standing for the overload's vector type.
VECTOR_BODIES = {
    "Normalize": "{V}.Normalize(v)",
    "Length": "v.Length()",
    "Dot": "{V}.Dot(a, b)",
    "Distance": "{V}.Distance(a, b)",
    "Reflect": "i - n * (2f * {V}.Dot(n, i))",
    "Refract": "RefractAbout(i, n, eta, {V}.Dot(n, i))",
    "Cross": "{V}.Cross(a, b)",
}

HELPERS = '''    private static float Hermite(float t)
    {
        return t * t * (3f - 2f * t);
    }

    // Zero past the critical angle, where no light is transmitted.
    private static Vector2 RefractAbout(Vector2 i, Vector2 n, float eta, float cosine)
    {
        var k = 1f - eta * eta * (1f - cosine * cosine);
        return k < 0f ? Vector2.Zero : i * eta - n * (eta * cosine + MathF.Sqrt(k));
    }

    private static Vector3 RefractAbout(Vector3 i, Vector3 n, float eta, float cosine)
    {
        var k = 1f - eta * eta * (1f - cosine * cosine);
        return k < 0f ? Vector3.Zero : i * eta - n * (eta * cosine + MathF.Sqrt(k));
    }

    private static Vector4 RefractAbout(Vector4 i, Vector4 n, float eta, float cosine)
    {
        var k = 1f - eta * eta * (1f - cosine * cosine);
        return k < 0f ? Vector4.Zero : i * eta - n * (eta * cosine + MathF.Sqrt(k));
    }
'''


def type_name(width):
    return "float" if width == 1 else f"Vector{width}"


def slang_name(name):
    # Slang's own builtins are all-lowercase (smoothstep, not smoothStep) - unlike Rin.Shade's own
    # camelCase field/method convention, which only lowercases the leading letter.
    return name.lower()


def per_component_body(name, width, params):
    """Calls the float overload once per component, passing float parameters through unchanged."""
    calls = []
    for component in COMPONENTS[:width]:
        args = ", ".join(pname if w is None else f"{pname}.{component}" for pname, w in params)
        calls.append(f"{name}({args})")
    return f"new({', '.join(calls)})"


def body_of(name, width, params):
    if name in VECTOR_BODIES:
        return VECTOR_BODIES[name].replace("{V}", type_name(width))
    if name == "Lerp":
        return "a + (b - a) * t"
    if width == 1:
        return SCALAR_BODIES[name]
    return per_component_body(name, width, params)


def declare(name, widths, params, return_of):
    """params: list of (csharp_param_name, width_or_None_for_float_scalar) where None means
    always float regardless of the overload's own width. return_of: 'self' (same as overload
    width) or 'float' (always scalar)."""
    lines = []
    for width in widths:
        t = type_name(width)
        arg_list = ", ".join(
            f"{'float' if w is None else t} {pname}" for pname, w in params
        )
        template_args = ", ".join(f"@{i}" for i in range(len(params)))
        return_type = "float" if return_of == "float" else t
        lines.append(f'    [SlangExpression("{slang_name(name)}({template_args})")]')
        lines.append(f"    public static {return_type} {name}({arg_list}) => {body_of(name, width, params)};")
    return lines


def main():
    lines = []

    # T f(x) - per-component, valid on a bare float too.
    for name in ["Abs", "Sign", "Floor", "Ceil", "Round", "Frac", "Sqrt", "Rsqrt",
                 "Saturate", "Exp", "Exp2", "Log", "Log2", "Sin", "Cos", "Tan", "Asin", "Acos", "Atan",
                 "Radians", "Degrees"]:
        lines += declare(name, ALL_WIDTHS, [("x", "self")], "self")
        lines.append("")

    # T f(a, b) - per-component, valid on a bare float too.
    for name in ["Min", "Max", "Pow", "Step", "Atan2"]:
        lines += declare(name, ALL_WIDTHS, [("a", "self"), ("b", "self")], "self")
        lines.append("")

    # T f(a, b, c) - per-component, valid on a bare float too.
    lines += declare("Clamp", ALL_WIDTHS, [("x", "self"), ("lo", "self"), ("hi", "self")], "self")
    lines.append("")
    lines += declare("SmoothStep", ALL_WIDTHS, [("edge0", "self"), ("edge1", "self"), ("x", "self")], "self")
    lines.append("")

    # T f(a, b, t) - t is always a scalar blend factor, even for vector a/b.
    lines += declare("Lerp", ALL_WIDTHS, [("a", "self"), ("b", "self"), ("t", None)], "self")
    lines.append("")

    # Vector-only: whole-vector ops, not meaningful on a bare float.
    lines += declare("Normalize", VECTOR_WIDTHS, [("v", "self")], "self")
    lines.append("")
    lines += declare("Length", VECTOR_WIDTHS, [("v", "self")], "float")
    lines.append("")
    lines += declare("Dot", VECTOR_WIDTHS, [("a", "self"), ("b", "self")], "float")
    lines.append("")
    lines += declare("Distance", VECTOR_WIDTHS, [("a", "self"), ("b", "self")], "float")
    lines.append("")
    lines += declare("Reflect", VECTOR_WIDTHS, [("i", "self"), ("n", "self")], "self")
    lines.append("")
    lines += declare("Refract", VECTOR_WIDTHS, [("i", "self"), ("n", "self"), ("eta", None)], "self")
    lines.append("")

    # Cross is only defined for 3-vectors.
    lines += declare("Cross", [3], [("a", "self"), ("b", "self")], "self")
    lines.append("")

    body = "\n".join(lines) + HELPERS

    header = '''using System.Numerics;

namespace Rin.Shade;

// [SlangExpression]-bound math builtins, one overload per width (generics aren't supported, SHADE0008).
// The transpiler emits the Slang expression and never reads a body. The bodies are what the same call
// computes when shader code runs as ordinary C#, in a test or on the CPU.
// Generated by scripts/gen_math_intrinsics.py - do not hand-edit, regenerate instead.
public abstract partial class Shader
{
'''
    footer = "}\n"

    content = header + body + footer

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(content, newline="\n")

    total = content.count("[SlangExpression(")
    print(f"wrote {OUTPUT_PATH} with {total} intrinsic overloads")


if __name__ == "__main__":
    main()
