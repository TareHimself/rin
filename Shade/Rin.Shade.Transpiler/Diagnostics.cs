using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal static class Diagnostics
{
    internal static class Emitter
    {
        public static readonly DiagnosticDescriptor MissingShaderBaseType = new(
            id: "SHADE0001",
            title: "Rin.Shade.Shader type not found",
            messageFormat: "Could not resolve 'Rin.Shade.Shader' in the compilation - reference Rin.Shade",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NoEntryPoint = new(
            id: "SHADE0002",
            title: "Missing entry point",
            messageFormat: "Shader class '{0}' has no [Compute] entry point method",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedConstruct = new(
            id: "SHADE0003",
            title: "Unsupported construct",
            messageFormat: "'{0}' is not supported in a shader body",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NoSourceForMethod = new(
            id: "SHADE0004",
            title: "No source available for method",
            messageFormat: "No source available for '{0}': add a [SlangCall] binding, or mark its file ShaderCompile",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RecursionNotSupported = new(
            id: "SHADE0005",
            title: "Recursion is not supported",
            messageFormat: "'{0}' is part of a recursive call cycle, which is not supported in a shader body",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedType = new(
            id: "SHADE0006",
            title: "Unsupported type",
            messageFormat: "'{0}' cannot be used in a shader: not a recognized built-in, struct, enum, BufferRef<T>, or fixed-size array",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ExtensionMethodNotSupported = new(
            id: "SHADE0007",
            title: "C# 14 extension methods are not supported",
            messageFormat: "'{0}' is a C# extension method - the receiver isn't in Parameters, so it can't be emitted correctly yet (extension properties, e.g. swizzles, are unaffected)",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GenericNotSupported = new(
            id: "SHADE0008",
            title: "Generics are not supported",
            messageFormat: "'{0}' is generic - different instantiations (e.g. Foo<int> and Foo<float>) would collide on the same emitted Slang name, since type arguments aren't part of it",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedEnumUnderlyingType = new(
            id: "SHADE0009",
            title: "Unsupported enum underlying type",
            messageFormat: "enum '{0}' has underlying type '{1}' - only the default (int) is supported, since a different size would silently disagree with the emitted Slang enum's layout",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultiDimensionalArrayNotSupported = new(
            id: "SHADE0010",
            title: "Multi-dimensional/jagged arrays are not supported",
            messageFormat: "'{0}' has more than one array dimension - only a single [FixedSize(N)] dimension is supported",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedSwitchClause = new(
            id: "SHADE0011",
            title: "Unsupported switch case clause",
            messageFormat: "'{0}' is not a supported switch case clause - only constant-value and default cases are supported, not pattern matching",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FieldShadowsCalledFunction = new(
            id: "SHADE0012",
            title: "Unqualified call resolves to a same-named field instead",
            messageFormat: "'{0}' is both a field on this struct and the target of this unqualified call - Slang resolves the call to the field instead (even for its own builtins, e.g. a field named 'min' shadows min(...)), which fails to compile; rename the field or the called function",
            category: "Rin.Shade",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
