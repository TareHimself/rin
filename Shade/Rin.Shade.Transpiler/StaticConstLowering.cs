using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// A `static readonly` field a shader reads, with the initializer that becomes its Slang `static const` value.
/// </summary>
internal sealed record StaticConstSpec(IFieldSymbol Field, IOperation Initializer);

/// <summary>
/// Lowers `static readonly` fields with constant initializers to Slang `static const` declarations. One declared
/// in a struct is printed inside that struct and read as `Type.name`; one declared in a class is printed in a
/// namespace named after the class, like a type nested in that class, and read through the name scope.
/// </summary>
internal static class StaticConstLowering
{
    /// <summary>
    /// Whether the field is a `static readonly` with an initializer, as opposed to a `const` (inlined), a
    /// binding field (declared by BindingLowering) or a mutable static (unsupported).
    /// </summary>
    public static bool IsStaticConst(IFieldSymbol field) =>
        field is { IsStatic: true, IsReadOnly: true, HasConstantValue: false } &&
        !BindingLowering.HasBindingGroupAttribute(field) &&
        !GroupSharedLowering.HasGroupSharedAttribute(field) &&
        field.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is VariableDeclaratorSyntax { Initializer: not null });

    /// <summary>
    /// The Slang expression that reads the constant from the current name scope.
    /// </summary>
    public static string Reference(IFieldSymbol field)
    {
        var name = Naming.ToSlangIdentifier(field.Name);
        return field.ContainingType.TypeKind == TypeKind.Struct
            ? $"{TypeMapping.MapType(field.ContainingType)}.{name}"
            : Naming.Qualify(field, name);
    }

    /// <summary>
    /// The element count of a static readonly array, read off its initializer syntax.
    /// </summary>
    public static int? ArrayLength(IFieldSymbol field)
    {
        foreach (var reference in field.DeclaringSyntaxReferences)
            if (reference.GetSyntax() is VariableDeclaratorSyntax { Initializer.Value: var value })
                return value switch
                {
                    InitializerExpressionSyntax initializer => initializer.Expressions.Count,
                    ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions.Count,
                    ImplicitArrayCreationExpressionSyntax { Initializer: var initializer } => initializer.Expressions.Count,
                    CollectionExpressionSyntax collection => collection.Elements.Count,
                    _ => null
                };

        return null;
    }

    /// <summary>
    /// The constants the methods read, each after the constants its own initializer reads, with their types
    /// added to the graph. Fields whose initializer is not a constant expression are reported and skipped.
    /// </summary>
    public static List<StaticConstSpec> Collect(IEnumerable<IMethodSymbol> methods, Compilation compilation,
        List<Diagnostic> diagnostics, TypeGraph types)
    {
        var collector = new Collector(compilation, diagnostics, types);
        foreach (var method in methods)
        foreach (var body in MethodSource.GetBodies(method, compilation))
            collector.Walk(body);
        return collector.Order;
    }

    /// <summary>
    /// The `static const` declaration line, lowered in the current name scope.
    /// </summary>
    public static string Declaration(StaticConstSpec spec, List<Diagnostic> diagnostics)
    {
        var lowering = new BodyLowering(diagnostics, new SlangWriter());
        var name = Naming.ToSlangIdentifier(spec.Field.Name);

        if (spec.Field.Type is IArrayTypeSymbol arrayType)
        {
            var elements = ArrayElements(spec.Initializer);
            var values = string.Join(", ", elements.Select(lowering.LowerExpression));
            return $"static const {TypeMapping.MapType(arrayType.ElementType)} {name}[{elements.Count}] = {{ {values} }};";
        }

        return $"static const {TypeMapping.MapType(spec.Field.Type)} {name} = {lowering.LowerExpression(spec.Initializer)};";
    }

    private static IReadOnlyList<IOperation> ArrayElements(IOperation initializer)
    {
        while (initializer is IConversionOperation { IsImplicit: true } conversion) initializer = conversion.Operand;

        return initializer switch
        {
            IArrayCreationOperation { Initializer: { } arrayInitializer } => arrayInitializer.ElementValues,
            IArrayInitializerOperation arrayInitializer => arrayInitializer.ElementValues,
            ICollectionExpressionOperation collection => collection.Elements,
            _ => []
        };
    }

    private static IOperation? InitializerOf(IFieldSymbol field, Compilation compilation)
    {
        foreach (var reference in field.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not VariableDeclaratorSyntax { Initializer: { } equalsValue }) continue;

            var model = compilation.GetSemanticModel(equalsValue.SyntaxTree);
            if (model.GetOperation(equalsValue) is IFieldInitializerOperation { Value: { } value }) return value;
        }

        return null;
    }

    // Slang requires a `static const` initializer to be a constant expression, so calls (even to intrinsics) and
    // user struct constructors are rejected rather than emitted and failed later by the Slang compiler.
    private static bool IsConstantExpression(IOperation operation) => operation switch
    {
        ILiteralOperation or IDefaultValueOperation => true,
        IFieldReferenceOperation { Instance: null, Field: var field } =>
            field.HasConstantValue || IsStaticConst(field) || field.ContainingType.TypeKind == TypeKind.Enum,
        IConversionOperation conversion => IsConstantExpression(conversion.Operand),
        IUnaryOperation unary => IsConstantExpression(unary.Operand),
        IBinaryOperation binary => IsConstantExpression(binary.LeftOperand) && IsConstantExpression(binary.RightOperand),
        IConditionalOperation { WhenFalse: { } whenFalse } conditional =>
            IsConstantExpression(conditional.Condition) && IsConstantExpression(conditional.WhenTrue) &&
            IsConstantExpression(whenFalse),
        IObjectCreationOperation { Type: { } type } creation when TypeMapping.IsIntrinsicVectorOrMatrixConstructor(type) =>
            creation.Arguments.All(a => IsConstantExpression(a.Value)),
        IArrayCreationOperation { Initializer: { } initializer } => initializer.ElementValues.All(IsConstantExpression),
        IArrayInitializerOperation initializer => initializer.ElementValues.All(IsConstantExpression),
        ICollectionExpressionOperation collection => collection.Elements.All(IsConstantExpression),
        _ => false
    };

    private sealed class Collector(Compilation compilation, List<Diagnostic> diagnostics, TypeGraph types)
    {
        private readonly HashSet<IFieldSymbol> _visited = new(SymbolEqualityComparer.Default);

        public List<StaticConstSpec> Order { get; } = [];

        // Stops at a local function, like FunctionCollector.FindReachableCallSites.
        public void Walk(IOperation operation)
        {
            if (operation is IFieldReferenceOperation { Instance: null, Field: var field } && IsStaticConst(field))
                Visit(field);
            if (operation is ILocalFunctionOperation) return;

            foreach (var child in operation.ChildOperations) Walk(child);
        }

        private void Visit(IFieldSymbol field)
        {
            if (!_visited.Add(field)) return;

            var location = field.Locations.FirstOrDefault() ?? Location.None;
            var initializer = InitializerOf(field, compilation);
            if (initializer is null)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct, location,
                    $"static readonly field '{field.Name}' with no initializer"));
                return;
            }

            Walk(initializer);

            var elementType = field.Type is IArrayTypeSymbol arrayType ? arrayType.ElementType : field.Type;
            if (!TypeMapping.IsLegalShaderType(elementType))
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedType, location,
                    $"{field.Type.ToDisplayString()} (static readonly field '{field.Name}')"));
                return;
            }

            if (!IsConstantExpression(initializer))
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct, location,
                    $"static readonly field '{field.Name}' whose initializer is not a constant expression (literals, vector and matrix constructors, operators and other constants only)"));
                return;
            }

            types.Add(field.Type);
            Order.Add(new StaticConstSpec(field, initializer));
        }
    }
}
