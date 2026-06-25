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
}