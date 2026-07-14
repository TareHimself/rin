using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Rin.Core.Generators;

[Generator]
public class AudioEffectGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "Rin.Core.Audio.Effects.AudioEffectAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter for classes or structs that might have the [AudioEffect] attribute
        var types = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax or StructDeclarationSyntax,
                transform: static (ctx, _) => GetTypeDeclarationForSourceGen(ctx))
            .Where(static t => t.typeDeclaration is not null);

        // Combine with compilation and execute generation
        context.RegisterSourceOutput(context.CompilationProvider.Combine(types.Collect()),
            static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    /// <summary>
    /// Checks if a type declaration has the [AudioEffect] attribute using semantic analysis.
    /// </summary>
    private static (TypeDeclarationSyntax? typeDeclaration, INamedTypeSymbol? attributeSymbol)
        GetTypeDeclarationForSourceGen(
            GeneratorSyntaxContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;

        foreach (var attributeList in typeDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                // Use SemanticModel to get the actual symbol of the attribute to avoid string matching pitfalls
                if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attribute).Symbol is IMethodSymbol
                    {
                        ContainingType: var attrType
                    } &&
                    attrType.ToDisplayString() == AttributeFullName)
                {
                    return (typeDeclaration, attrType);
                }
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Core execution of the source generator.
    /// </summary>
    private static void Execute(SourceProductionContext context, Compilation compilation,
        ImmutableArray<(TypeDeclarationSyntax? typeDeclaration, INamedTypeSymbol? attributeSymbol)> types)
    {
        if (types.IsDefaultOrEmpty) return;

        foreach (var item in types.Distinct())
        {
            if (item.typeDeclaration == null || item.attributeSymbol == null) continue;

            var semanticModel = compilation.GetSemanticModel(item.typeDeclaration.SyntaxTree);
            if (ModelExtensions.GetDeclaredSymbol(semanticModel, item.typeDeclaration) is not INamedTypeSymbol
                typeSymbol)
                continue;

            var attributeData = typeSymbol.GetAttributes()
                .FirstOrDefault(c => c.AttributeClass?.ToDisplayString() == AttributeFullName);

            if (attributeData == null) continue;

            GenerateEffect(context, typeSymbol, compilation);
        }
    }

    static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var syntaxRef in type.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDecl)
            {
                if (typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds a filesystem-safe, collision-resistant hint name for a generated source file
    /// from a type's fully-qualified name (handles generics, nested types, and namespace clashes).
    /// </summary>
    private static string GetSafeHintName(INamedTypeSymbol typeSymbol)
    {
        var fullyQualified = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var sb = new StringBuilder(fullyQualified.Length);
        foreach (var c in fullyQualified)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates the boilerplate for a specific audio effect.
    /// </summary>
    private static void GenerateEffect(SourceProductionContext context, INamedTypeSymbol typeSymbol,
        Compilation compilation)
    {
        if (!IsPartial(typeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Audio.EffectMustBePartial,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.ToDisplayString()
                )
            );
            return;
        }

        // Resolve candidate "Process" methods. Disambiguate overloads explicitly rather than
        // silently picking whichever one GetMembers() happens to return first.
        var processCandidates = typeSymbol.GetMembers("Process").OfType<IMethodSymbol>().ToImmutableArray();

        if (processCandidates.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Audio.MissingProcessMethod,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.ToDisplayString()
                )
            );
            return;
        }

        if (processCandidates.Length > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Audio.AmbiguousProcessMethod,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.ToDisplayString()
                )
            );
            return;
        }

        var processMethod = processCandidates[0];

        // The generated UnmanagedCallersOnly shim invokes Process as a static member.
        // An instance method here would compile in the generator but fail in generated code.
        if (!processMethod.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Audio.ProcessMethodMustBeStatic,
                    processMethod.Locations.FirstOrDefault() ?? typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.ToDisplayString()
                )
            );
            return;
        }

        var contextIndex = -1;
        var stateIndex = -1;
        var parametersIndex = -1;
        var hasInput = false;
        var hasOutput = false;
        var floatType = compilation.GetSpecialType(SpecialType.System_Single);
        var noAudioEffectParametersType =
            compilation.GetTypeByMetadataName("Rin.Core.Audio.Effects.NoAudioEffectParameters");
        var audioContextType = compilation.GetTypeByMetadataName("Rin.Core.Audio.Effects.AudioEffectContext");
        var effectDescriptorType = compilation.GetTypeByMetadataName("Rin.Core.Audio.Effects.IAudioEffectDescriptor");
        var memoryType = compilation.GetTypeByMetadataName("Rin.Core.Memory");
        var allocateMethod = memoryType?
            .GetMembers("Allocate")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic);
        var freeMethod = memoryType?
            .GetMembers("Free")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic);
        if (audioContextType == null || noAudioEffectParametersType == null || effectDescriptorType == null || allocateMethod == null ||
            freeMethod == null) return;

        for (var i = 0; i < processMethod.Parameters.Length; i++)
        {
            var parameter = processMethod.Parameters[i];
            switch (parameter.Name)
            {
                case "ctx":
                {
                    if (parameter.Type is INamedTypeSymbol s)
                    {
                        if (SymbolEqualityComparer.Default.Equals(s.OriginalDefinition, audioContextType))
                        {
                            contextIndex = i;
                            continue;
                        }
                    }

                    context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.Audio.InvalidTypeForContext,
                            parameter.Locations.FirstOrDefault()
                        )
                    );
                    return;
                }
                case "state":
                {
                    if (!parameter.Type.IsUnmanagedType)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                                Diagnostics.Audio.TypeMustBeUnManaged,
                                parameter.Locations.FirstOrDefault(),
                                parameter.Type.ToDisplayString()
                            )
                        );
                        return;
                    }

                    if (parameter.RefKind != RefKind.Ref)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                                Diagnostics.Audio.ParameterMustBePassedByRef,
                                parameter.Locations.FirstOrDefault(),
                                parameter.ToDisplayString()
                            )
                        );
                        return;
                    }

                    stateIndex = i;
                    continue;
                }
                case "parameters":
                {
                    if (!parameter.Type.IsUnmanagedType)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                                Diagnostics.Audio.TypeMustBeUnManaged,
                                parameter.Locations.FirstOrDefault(),
                                parameter.Type.ToDisplayString()
                            )
                        );
                        return;
                    }

                    if (parameter.RefKind != RefKind.In)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                                Diagnostics.Audio.ParameterMustBePassedByRef,
                                parameter.Locations.FirstOrDefault(),
                                parameter.ToDisplayString()
                            )
                        );
                        return;
                    }

                    parametersIndex = i;
                    continue;
                }
                case "input":
                {
                    if (parameter.Type is INamedTypeSymbol s)
                    {
                        var readOnlySpanType = compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");
                        if (SymbolEqualityComparer.Default.Equals(s.OriginalDefinition, readOnlySpanType) &&
                            s.TypeArguments.Length == 1 &&
                            SymbolEqualityComparer.Default.Equals(s.TypeArguments[0], floatType))
                        {
                            hasInput = true;
                            continue;
                        }
                    }

                    context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.Audio.InvalidInputType,
                            parameter.Locations.FirstOrDefault()
                        )
                    );
                    return;
                }
                case "output":
                {
                    if (parameter.Type is INamedTypeSymbol s)
                    {
                        var spanType = compilation.GetTypeByMetadataName("System.Span`1");
                        if (SymbolEqualityComparer.Default.Equals(s.OriginalDefinition, spanType) &&
                            s.TypeArguments.Length == 1 &&
                            SymbolEqualityComparer.Default.Equals(s.TypeArguments[0], floatType))
                        {
                            hasOutput = true;
                            continue;
                        }
                    }

                    context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.Audio.InvalidOutputType,
                            parameter.Locations.FirstOrDefault()
                        )
                    );
                    return;
                }
                default:
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.Audio.InvalidArgument,
                            parameter.Locations.FirstOrDefault()
                        )
                    );
                    return;
                }
            }
        }

        // An audio effect that never touches its input/output buffers is almost certainly a
        // mistake (missing parameter, typo'd name, etc). Require both explicitly.
        if (!hasInput || !hasOutput)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Audio.MissingInputOrOutputParameter,
                    processMethod.Locations.FirstOrDefault() ?? typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.ToDisplayString()
                )
            );
            return;
        }

        var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
        var typeName = typeSymbol.Name;
        var kind = typeSymbol.IsValueType ? "struct" : "class";

        var output = new SourceBuilder();
        var callArguments = processMethod.Parameters.Select(c => c.Name switch
        {
            "input" => "inSpan",
            "output" => "outSpan",
            "ctx" => "inContext",
            "parameters" => "inParameters",
            "state" => "ref inState",
            _ => throw new ArgumentOutOfRangeException()
        });

        output
            .Line("// <auto-generated/>")
            .Line()
            .Line($"namespace {namespaceName};")
            .Line($"partial {kind} {typeName}")
            .OpenBrace()
            .Line()
            .Line(
                "[global::System.Runtime.InteropServices.UnmanagedCallersOnly]")
            .Line(
                $"public static unsafe void ProcessNative(float* input, float* output, int sampleCount, {audioContextType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}* ctx, void* parameters, void* state)")
            .OpenBrace()
            .Line("var inSpan = new global::System.ReadOnlySpan<float>(input, sampleCount);")
            .Line("var outSpan = new global::System.Span<float>(output, sampleCount);");

        if (contextIndex != -1)
        {
            output.Line(
                $"ref readonly var inContext = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<{processMethod.Parameters[contextIndex].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(ctx);");
        }

        if (stateIndex != -1)
        {
            output.Line(
                $"ref var inState = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<{processMethod.Parameters[stateIndex].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(state);");
        }

        if (parametersIndex != -1)
        {
            output.Line(
                $"ref readonly var inParameters = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<{processMethod.Parameters[parametersIndex].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(parameters);");
        }

        output
            .Line()
            .Line($"{processMethod.ContainingType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat)}.{processMethod.Name}" +
                  $"({string.Join(", ", callArguments)});")
            .CloseBrace();

        // Create struct that implements effect descriptor interface
        {
            var descriptorBase =
                $"{effectDescriptorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}<{(parametersIndex == -1 ? noAudioEffectParametersType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : processMethod.Parameters[parametersIndex].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}>";

            output
                .Line($"internal class EffectDescriptor : {descriptorBase}")
                .OpenBrace()
                .Line("public IntPtr GetProcessMethodPtr()")
                .OpenBrace()
                .OpenBrace("unsafe")
                .Line(
                    $"delegate* unmanaged<float*, float*, int, {audioContextType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}*, void*, void*, void> fnPtr = &ProcessNative;")
                .Line("return (IntPtr)fnPtr;")
                .CloseBrace()
                .CloseBrace();

            if (stateIndex != -1)
            {
                var stateType = processMethod.Parameters[stateIndex].Type
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                output
                    .Line()
                    .Line("public IntPtr CreateState()")
                    .OpenBrace()
                    .OpenBrace("unsafe")
                    .Line(
                        $"var ptr = {allocateMethod.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{allocateMethod.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}((ulong)sizeof({stateType}));")
                    .Line($"global::System.Runtime.CompilerServices.Unsafe.Write(ptr.ToPointer(), new {stateType}());")
                    .Line("return ptr;")
                    .CloseBrace()
                    .CloseBrace()
                    .Line()
                    .Line("public void ReleaseState(IntPtr state)")
                    .OpenBrace()
                    .Line($"{freeMethod.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{freeMethod.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(state);")
                    .CloseBrace();
            }
            else
            {
                output
                    .Line()
                    .Line("public IntPtr CreateState()")
                    .OpenBrace()
                    .Line("return IntPtr.Zero;")
                    .CloseBrace()
                    .Line()
                    .Line("public void ReleaseState(IntPtr state)")
                    .OpenBrace()
                    .CloseBrace();
            }

            if (parametersIndex != -1)
            {
                var paramsType = processMethod.Parameters[parametersIndex].Type
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                output
                    .Line()
                    .Line("public IntPtr CreateParameters()")
                    .OpenBrace()
                    .OpenBrace("unsafe")
                    .Line(
                        $"var ptr = {allocateMethod.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{allocateMethod.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}((ulong)sizeof({paramsType}));")
                    .Line($"global::System.Runtime.CompilerServices.Unsafe.Write(ptr.ToPointer(), new {paramsType}());")
                    .Line("return ptr;")
                    .CloseBrace()
                    .CloseBrace()
                    .Line()
                    .Line("public void ReleaseParameters(IntPtr parameters)")
                    .OpenBrace()
                    .Line($"{freeMethod.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{freeMethod.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(parameters);")
                    .CloseBrace();
            }
            else
            {
                output
                    .Line()
                    .Line("public IntPtr CreateParameters()")
                    .OpenBrace()
                    .Line("return IntPtr.Zero;")
                    .CloseBrace()
                    .Line()
                    .Line("public void ReleaseParameters(IntPtr parameters)")
                    .OpenBrace()
                    .CloseBrace();
            }

            output
                .CloseBrace()
                .Line(
                    $"public static readonly {descriptorBase} Descriptor = new EffectDescriptor();");
        }

        output
            .CloseBrace();

        context.AddSource($"AudioEffect_{GetSafeHintName(typeSymbol)}.g.cs", output.ToSourceText());
    }
}