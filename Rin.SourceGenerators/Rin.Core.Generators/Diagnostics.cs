using Microsoft.CodeAnalysis;

namespace Rin.Core.Generators;

internal static class Diagnostics
{
    internal static class Audio
    {
        public static readonly DiagnosticDescriptor EffectMustBePartial = new(
            id: "RIN00001",
            title: "Audio effect must be partial",
            messageFormat: "Audio effect '{0}' must be declared partial so generated code can extend it",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingProcessMethod = new(
            id: "RIN00002",
            title: "Missing Process method",
            messageFormat: "Audio effect '{0}' must declare a valid static Process method",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidInputType = new(
            id: "RIN00003",
            title: "Invalid input type",
            messageFormat: "input must be of type ReadOnlySpan<float>",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidOutputType = new(
            id: "RIN00004",
            title: "Invalid output type",
            messageFormat: "output must be of type Span<float>",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidArgument = new(
            id: "RIN00005",
            title: "Invalid argument",
            messageFormat: "Invalid argument",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidTypeForContext = new(
            id: "RIN00006",
            title: "Invalid type for context",
            messageFormat: "Type of context must be AudioEffectContext",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TypeMustBeUnManaged = new(
            id: "RIN00007",
            title: "Type is not unmanaged",
            messageFormat: "Type '{0}' must be unmanaged",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ParameterMustBePassedByRef = new(
            id: "RIN00008",
            title: "Parameter must be passed by ref",
            messageFormat: "Parameter '{0}' must be passed by ref",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ProcessMethodMustBeStatic = new(
            id: "RIN00009",
            title: "Process method must be static",
            messageFormat: "Audio effect '{0}' Process method must be static",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingInputOrOutputParameter = new(
            id: "RIN00010",
            title: "Missing input or output parameter",
            messageFormat:
            "Audio effect '{0}' Process method must declare both an input (ReadOnlySpan<float>) and an output (Span<float>) parameter",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AmbiguousProcessMethod = new(
            id: "RIN00011",
            title: "Ambiguous Process method",
            messageFormat: "Audio effect '{0}' declares multiple Process methods; only one overload is allowed",
            category: "Rin.Audio",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }

    internal static class Shader
    {
        public static readonly DiagnosticDescriptor AmbiguousShaderAttribute = new(
            id: "RIN00012",
            title: "Ambiguous shader attribute",
            messageFormat: "Property '{0}' declares multiple ShaderAttribute-derived attributes; only one is allowed",
            category: "Rin.Graphics",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
            id: "RIN00013",
            title: "Containing type must be partial",
            messageFormat: "Type '{0}' containing shader property '{1}' must be declared partial so generated code can extend it",
            category: "Rin.Graphics",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PropertyMustBePartial = new(
            id: "RIN00014",
            title: "Shader property must be partial",
            messageFormat: "Property '{0}' must be declared partial so the generator can provide its implementation",
            category: "Rin.Graphics",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PropertyMustBeGetOnly = new(
            id: "RIN00015",
            title: "Shader property must be get-only",
            messageFormat: "Property '{0}' must declare a getter only (no setter/init) - the generator produces a computed value",
            category: "Rin.Graphics",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidPropertyType = new(
            id: "RIN00016",
            title: "Invalid shader property type",
            messageFormat: "Property '{0}' annotated with [{1}] must be of type {2}",
            category: "Rin.Graphics",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PropertyMustNotBeStatic = new(
            id: "RIN00017",
            title: "Shader property must not be static",
            messageFormat: "Property '{0}' must be an instance property - static shader properties are not supported",
            category: "Rin.Graphics",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}