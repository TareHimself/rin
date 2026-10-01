using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// v1 statement/expression set: block, if/for/while/switch, fixed-size-array foreach, return,
/// local declaration ('var' only), assignment/compound-assignment, field/parameter/local
/// reference, BufferRef&lt;T&gt; indexer, swizzles, binary/unary/ternary operators, matrix mul(),
/// literals, casts, `with` (WithLowering), and [SlangExpression]/[SlangStatement] invocations. A local
/// function is inlined. Anything else is a diagnostic, not a crash.
/// </summary>
internal sealed class BodyLowering(
    List<Diagnostic> diagnostics, SlangWriter writer, IReadOnlySet<string>? shadowableFieldNames = null,
    IReadOnlyDictionary<string, WithHelperSpec>? withHelpers = null,
    IReadOnlyDictionary<IMethodSymbol, IMethodSymbol>? overrides = null)
{
    private int _loopDepth;

    public void LowerStatement(IOperation? op)
    {
        if (op is not null) DeclareOutVariables(op);

        switch (op)
        {
            case null:
                return;
            // A braced block of its own (a case body, or a bare { } inside another block) is a scope: two cases
            // that each declare `data` would otherwise declare it twice in the one switch.
            case IBlockOperation { Parent: ISwitchCaseOperation or IBlockOperation } scoped:
                writer.OpenBrace();
                foreach (var statement in scoped.Operations) LowerStatement(statement);
                writer.CloseBrace();
                return;
            case IBlockOperation block:
                foreach (var statement in block.Operations) LowerStatement(statement);
                return;
            case ILocalFunctionOperation:
                // Inlined: nothing to emit here. FunctionCollector/FunctionLowering walk and emit
                // its body separately, wherever it's actually called.
                return;
            case IConditionalOperation conditional:
                writer.OpenBrace($"if ({LowerExpr(conditional.Condition)})");
                LowerStatement(conditional.WhenTrue);
                writer.CloseBrace();
                if (conditional.WhenFalse is not null)
                {
                    writer.OpenBrace("else");
                    LowerStatement(conditional.WhenFalse);
                    writer.CloseBrace();
                }
                return;
            case IForLoopOperation forLoop:
            {
                var before = string.Join(", ", forLoop.Before.Select(LowerLoopClause));
                var condition = forLoop.Condition is not null ? LowerExpr(forLoop.Condition) : "";
                var after = string.Join(", ", forLoop.AtLoopBottom.Select(LowerLoopClause));
                writer.OpenBrace($"for ({before}; {condition}; {after})");
                _loopDepth++;
                LowerStatement(forLoop.Body);
                _loopDepth--;
                writer.CloseBrace();
                return;
            }
            case IWhileLoopOperation { ConditionIsTop: true } whileLoop:
                writer.OpenBrace($"while ({LowerExpr(whileLoop.Condition!)})");
                _loopDepth++;
                LowerStatement(whileLoop.Body);
                _loopDepth--;
                writer.CloseBrace();
                return;
            case ISwitchOperation switchOp:
            {
                writer.OpenBrace($"switch ({LowerExpr(switchOp.Value)})");
                foreach (var switchCase in switchOp.Cases)
                {
                    var wroteLabel = false;
                    foreach (var clause in switchCase.Clauses)
                    {
                        if (clause is ISingleValueCaseClauseOperation { Value: { } value })
                        {
                            writer.Line($"case {LowerExpr(value)}:");
                            wroteLabel = true;
                        }
                        else if (clause is IDefaultCaseClauseOperation)
                        {
                            writer.Line("default:");
                            wroteLabel = true;
                        }
                        else
                        {
                            // A pattern-matching clause (case SomeType t:, case > 5:, etc.) - not
                            // emitting a label here but still emitting the case's body would produce
                            // statements floating with no case/default before them, silently
                            // corrupting the switch rather than failing loudly.
                            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedSwitchClause,
                                clause.Syntax.GetLocation(), clause.Kind.ToString()));
                        }
                    }

                    if (wroteLabel)
                        foreach (var body in switchCase.Body) LowerStatement(body);
                }
                writer.CloseBrace();
                return;
            }
            case IForEachLoopOperation forEach:
                LowerFixedArrayForEach(forEach);
                return;
            case IBranchOperation { BranchKind: BranchKind.Break }:
                writer.Line("break;");
                return;
            case IBranchOperation { BranchKind: BranchKind.Continue }:
                writer.Line("continue;");
                return;
            case IReturnOperation ret:
                writer.Line(ret.ReturnedValue is null ? "return;" : $"return {LowerExpr(ret.ReturnedValue)};");
                return;
            case IVariableDeclarationGroupOperation group:
                foreach (var declaration in group.Declarations)
                foreach (var declarator in declaration.Declarators)
                {
                    var initializer = declarator.Initializer?.Value;
                    var name = Naming.ToSlangIdentifier(declarator.Symbol.Name);
                    if (InlineArrays.TryGet(declarator.Symbol.Type, out var arrayElement, out var arrayLength))
                    {
                        var arrayInitializer = initializer is null || IsZeroInitializer(initializer)
                            ? "{}"
                            : LowerExpr(initializer);
                        writer.Line($"{TypeMapping.MapType(arrayElement)} {name}[{arrayLength}] = {arrayInitializer};");
                        continue;
                    }

                    if (declarator.Symbol.RefKind == RefKind.Ref)
                    {
                        LowerRefLocal(declarator, initializer, name);
                        continue;
                    }

                    if (declarator.Symbol.Type is IArrayTypeSymbol arrayType)
                    {
                        LowerArrayLocal(declarator, arrayType, initializer, name);
                        continue;
                    }

                    // 'var' needs a right-hand side to infer from - an uninitialized declaration
                    // (e.g. a pre-declared 'out' argument) has to spell out its real type instead.
                    //
                    // Likewise a constant whose C# type isn't int: Slang literals are emitted bare (`0`,
                    // not `0u` or `0.0`), so `var` would infer int and silently change the local's type.
                    var needsExplicitType = initializer is null ||
                                            (initializer.ConstantValue.HasValue &&
                                             declarator.Symbol.Type.SpecialType != SpecialType.System_Int32 &&
                                             TypeMapping.IsBuiltIn(declarator.Symbol.Type));
                    writer.Line(initializer is null
                        ? $"{TypeMapping.MapType(declarator.Symbol.Type)} {name};"
                        : needsExplicitType
                            ? $"{TypeMapping.MapType(declarator.Symbol.Type)} {name} = {LowerExpr(initializer)};"
                            : $"var {name} = {LowerExpr(initializer)};");
                }
                return;
            case IExpressionStatementOperation expressionStatement:
                LowerExpressionStatement(expressionStatement.Operation);
                return;
            default:
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    op.Syntax.GetLocation(), op.Kind.ToString()));
                return;
        }
    }

    private void LowerExpressionStatement(IOperation operation)
    {
        // [SlangStatement] (e.g. "discard;") substitutes the whole line, not an expression embedded
        // in one - the template already includes its own trailing ';'.
        if (operation is IInvocationOperation invocation)
        {
            var statementTemplate = IntrinsicBindings.GetSlangStatementTemplate(invocation.TargetMethod);
            if (statementTemplate is not null)
            {
                writer.Line(SubstituteTemplate(statementTemplate, invocation));
                return;
            }
        }

        writer.Line($"{LowerExpr(operation)};");
    }

    // Only `T[] x = new T[N]` / `new T[] { ... }` with a compile-time N is a legal array in a shader
    // body: Slang has no unsized or heap arrays, so anything else is diagnosed rather than lowered.
    private void LowerArrayLocal(IVariableDeclaratorOperation declarator, IArrayTypeSymbol arrayType,
        IOperation? initializer, string name)
    {
        while (initializer is IConversionOperation { IsImplicit: true } conversion) initializer = conversion.Operand;

        if (arrayType is not { Rank: 1, ElementType: not IArrayTypeSymbol } ||
            !TypeMapping.IsLegalShaderType(arrayType.ElementType) ||
            initializer is not IArrayCreationOperation creation)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedType,
                declarator.Syntax.GetLocation(),
                $"{arrayType.ToDisplayString()} local (only 'new T[N]' or 'new T[] {{ ... }}' of a single dimension is supported)"));
            return;
        }

        var elements = creation.Initializer?.ElementValues ?? [];
        if (creation.DimensionSizes[0].ConstantValue is not { HasValue: true, Value: int length })
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedType,
                creation.Syntax.GetLocation(), "array with a non-constant length (Slang arrays are fixed-size)"));
            return;
        }

        var initializerText = elements.IsEmpty
            ? "{}"
            : $"{{ {string.Join(", ", elements.Select(LowerExpr))} }}";
        writer.Line($"{TypeMapping.MapType(arrayType.ElementType)} {name}[{length}] = {initializerText};");
    }

    // `ref var q = ref buffer[i]` becomes a pointer local. A pointer into function-local storage
    // isn't something the emitted SPIR-V can be trusted with, so only a buffer element (or another
    // ref local) may be aliased.
    private void LowerRefLocal(IVariableDeclaratorOperation declarator, IOperation? initializer, string name)
    {
        while (initializer is IConversionOperation { IsImplicit: true } conversion) initializer = conversion.Operand;

        if (initializer is null || !TryGetAddress(initializer, out var address))
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                declarator.Syntax.GetLocation(), "ref local (only a BufferRef element can be aliased)"));
            return;
        }

        writer.Line($"var {name} = {address};");
    }

    private static bool IsZeroInitializer(IOperation initializer)
    {
        while (initializer is IConversionOperation { IsImplicit: true } conversion) initializer = conversion.Operand;
        return initializer is IDefaultValueOperation or IObjectCreationOperation { Arguments.Length: 0, Initializer: null };
    }

    private void LowerFixedArrayForEach(IForEachLoopOperation forEach)
    {
        var collection = forEach.Collection is IConversionOperation { IsImplicit: true } conversion
            ? conversion.Operand
            : forEach.Collection;

        if (collection is IFieldReferenceOperation fieldRef &&
            InlineArrays.TryGet(fieldRef.Field.Type, out _, out var size))
        {
            var index = $"i{_loopDepth}";
            var elementName = forEach.LoopControlVariable is IVariableDeclaratorOperation declarator
                ? Naming.ToSlangIdentifier(declarator.Symbol.Name)
                : "item";

            writer.OpenBrace($"for (var {index} = 0; {index} < {size}; {index}++)");
            writer.Line($"var {elementName} = {LowerExpr(fieldRef)}[{index}];");
            _loopDepth++;
            LowerStatement(forEach.Body);
            _loopDepth--;
            writer.CloseBrace();
            return;
        }

        diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
            forEach.Syntax.GetLocation(), "foreach (only [InlineArray] fields are supported)"));
    }

    private string LowerLoopClause(IOperation op) => op switch
    {
        IVariableDeclarationGroupOperation group => string.Join(", ", group.Declarations
            .SelectMany(d => d.Declarators)
            .Select(d => $"var {Naming.ToSlangIdentifier(d.Symbol.Name)} = {LowerExpr(d.Initializer!.Value)}")),
        IExpressionStatementOperation stmt => LowerExpr(stmt.Operation),
        _ => LowerExpr(op)
    };

    // Roslyn lists a call's arguments in the order they were written, so `F(b: 1, a: 2)` is [b, a]; Slang
    // takes them by position, so they go out in parameter order. Known quirk: C# evaluates arguments in written
    // order, so a call with side-effecting named arguments evaluates them in parameter order here instead.
    private static IArgumentOperation[] InParameterOrder(IEnumerable<IArgumentOperation> arguments) =>
        arguments.OrderBy(argument => argument.Parameter?.Ordinal ?? int.MaxValue).ToArray();

    // `out var x` declares x inside the call, which Slang can't do, so the declaration is written on the line
    // before the statement that contains it and the call passes the bare name.
    private void DeclareOutVariables(IOperation statement)
    {
        foreach (var declaration in FindOutDeclarations(statement))
            if (declaration.Expression is ILocalReferenceOperation local)
                writer.Line($"{TypeMapping.MapType(local.Local.Type)} {Naming.ToSlangIdentifier(local.Local.Name)};");
    }

    private static IEnumerable<IDeclarationExpressionOperation> FindOutDeclarations(IOperation parent)
    {
        foreach (var child in parent.ChildOperations)
        {
            if (IsStatement(child)) continue;

            if (child is IDeclarationExpressionOperation declaration) yield return declaration;
            foreach (var nested in FindOutDeclarations(child)) yield return nested;
        }
    }

    private static bool IsStatement(IOperation op) =>
        op is IBlockOperation or ISwitchCaseOperation or ILocalFunctionOperation or IExpressionStatementOperation
            or IReturnOperation or IVariableDeclarationGroupOperation or ILoopOperation or ISwitchOperation
            or IBranchOperation or IConditionalOperation { Type: null };

    private string LowerExpr(IOperation op)
    {
        switch (op)
        {
            case IDeclarationExpressionOperation declaration:
                return LowerExpr(declaration.Expression);
            case IParameterReferenceOperation parameter:
                return Naming.ToSlangIdentifier(parameter.Parameter.Name);
            case ILocalReferenceOperation { Local.RefKind: RefKind.Ref } refLocal:
                return $"(*{Naming.ToSlangIdentifier(refLocal.Local.Name)})";
            case ILocalReferenceOperation local:
                return Naming.ToSlangIdentifier(local.Local.Name);
            case IIncrementOrDecrementOperation inc:
                return $"{LowerExpr(inc.Target)}{(inc.Kind == OperationKind.Increment ? "++" : "--")}";
            case IFieldReferenceOperation field:
                return LowerFieldReference(field);
            case IPropertyReferenceOperation property when IsBufferRefIndexer(property.Property):
                return $"{LowerAtLeast(property.Instance!, PostfixPrecedence)}[{LowerExpr(property.Arguments[0].Value)}]";
            case IArrayElementReferenceOperation { Indices: [var arrayIndex] } arrayElement:
                return $"{LowerAtLeast(arrayElement.ArrayReference, PostfixPrecedence)}[{LowerExpr(arrayIndex)}]";
            case IInlineArrayAccessOperation inlineAccess:
                return $"{LowerAtLeast(inlineAccess.Instance, PostfixPrecedence)}[{LowerExpr(inlineAccess.Argument)}]";
            case IPropertyReferenceOperation { Instance: not null } autoProperty
                when StructMembers.IsAutoProperty(autoProperty.Property):
                return autoProperty.Instance is IInstanceReferenceOperation
                    ? Naming.ToSlangIdentifier(autoProperty.Property.Name)
                    : $"{LowerAtLeast(autoProperty.Instance, PostfixPrecedence)}.{Naming.ToSlangIdentifier(autoProperty.Property.Name)}";
            case IPropertyReferenceOperation property when IsSwizzle(property.Property):
                return $"{LowerAtLeast(property.Instance!, PostfixPrecedence)}.{Naming.ToSlangIdentifier(property.Property.Name)}";
            case IPropertyReferenceOperation { Property.GetMethod: { } getMethod } property
                when MethodSource.HasBody(getMethod):
                return LowerPropertyGet(property, getMethod);
            case ILiteralOperation { ConstantValue: { HasValue: true, Value: var value } }:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
            case IConversionOperation conversion:
                return conversion.IsImplicit
                    ? LowerExpr(conversion.Operand)
                    : $"({TypeMapping.MapType(conversion.Type!)}){LowerAtLeast(conversion.Operand, UnaryPrecedence)}";
            case IBinaryOperation binary when IsMatrixMultiply(binary):
                return $"mul({LowerExpr(binary.LeftOperand)}, {LowerExpr(binary.RightOperand)})";
            case IBinaryOperation { OperatorMethod.DeclaringSyntaxReferences.Length: > 0 } binary:
                // A BCL operator (Vector2.op_Subtraction etc.) has no declaring syntax in this
                // compilation and is trusted to mean the same thing in Slang - an operator declared
                // in source here is user-authored and needs its own [SlangExpression] binding rather than
                // being silently treated as a passthrough.
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    binary.Syntax.GetLocation(), $"user-defined operator overload '{binary.OperatorMethod!.Name}'"));
                return "/* unsupported */";
            case IBinaryOperation binary when TryMapOperator(binary.OperatorKind, out var operatorText):
            {
                var left = LowerBinaryOperand(binary.LeftOperand, binary.OperatorKind, false);
                var right = LowerBinaryOperand(binary.RightOperand, binary.OperatorKind, true);

                // The emitted literal for 2f is `2`, so `2f / 3f` would divide integers in Slang and give 0.
                if (binary.OperatorKind is BinaryOperatorKind.Divide or BinaryOperatorKind.Remainder &&
                    binary.Type is { SpecialType: SpecialType.System_Single or SpecialType.System_Double })
                {
                    left = AsFloatLiteral(left);
                    right = AsFloatLiteral(right);
                }

                return $"{left} {operatorText} {right}";
            }
            case IUnaryOperation { OperatorMethod.DeclaringSyntaxReferences.Length: > 0 } unary:
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    unary.Syntax.GetLocation(), $"user-defined operator overload '{unary.OperatorMethod!.Name}'"));
                return "/* unsupported */";
            case IUnaryOperation unary when TryMapUnaryOperator(unary.OperatorKind, out var operatorText):
                return $"{operatorText}{LowerAtLeast(unary.Operand, UnaryPrecedence)}";
            case ICompoundAssignmentOperation { OperatorMethod.DeclaringSyntaxReferences.Length: > 0 } compound:
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    compound.Syntax.GetLocation(),
                    $"user-defined operator overload '{compound.OperatorMethod!.Name}'"));
                return "/* unsupported */";
            case ICompoundAssignmentOperation compound when TryMapOperator(compound.OperatorKind, out var operatorText):
                return $"{LowerExpr(compound.Target)} {operatorText}= {LowerExpr(compound.Value)}";
            case ISimpleAssignmentOperation { Target: IPropertyReferenceOperation property } assignment
                when !IsBufferRefIndexer(property.Property) && !IsSwizzle(property.Property):
                return LowerPropertySet(property, assignment.Value);
            case ISimpleAssignmentOperation assignment:
                return $"{LowerExpr(assignment.Target)} = {LowerExpr(assignment.Value)}";
            // Ternary and `if` share IConditionalOperation; LowerStatement handles the if-shape first.
            case IConditionalOperation { WhenFalse: not null } conditional:
                return $"{LowerAtLeast(conditional.Condition, TernaryPrecedence + 1)} ? {LowerExpr(conditional.WhenTrue)} : {LowerExpr(conditional.WhenFalse)}";
            case IInvocationOperation invocation:
                return LowerInvocation(invocation);
            case IObjectCreationOperation creation:
                return LowerObjectCreation(creation);
            case IWithOperation withOperation:
                return LowerWith(withOperation);
            default:
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    op.Syntax.GetLocation(), op.Kind.ToString()));
                return "/* unsupported */";
        }
    }

    // A variant field has no storage of its own in Slang - it is the same bytes as the union's
    // header-plus-payload struct for that path, so reading it reinterprets the root instance as that
    // struct and takes the payload. A chain of variants (foo.Bar.Car) collapses into one
    // reinterpret of the root as Foo_Bar_Car, with one .payload per level.
    private string LowerVariantAccess(IFieldReferenceOperation field)
    {

        if (field.Field.Type is INamedTypeSymbol nestedUnion && UnionLayout.IsUnion(nestedUnion) &&
            field.Parent is not IFieldReferenceOperation)
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                field.Syntax.GetLocation(),
                $"copying nested union variant '{field.Field.Name}' (access one of its members directly)"));

        var chain = new List<UnionVariant>();
        var current = field;
        while (true)
        {
            var info = UnionLayout.Analyze(current.Field.ContainingType, []);
            chain.Insert(0, info!.Variants.First(v =>
                SymbolEqualityComparer.Default.Equals(v.Member.Field, current.Field)));

            if (current.Instance is IFieldReferenceOperation parent && UnionLayout.IsVariant(parent.Field))
                current = parent;
            else
                break;
        }

        var rootType = current.Field.ContainingType;
        var structName = UnionLowering.QualifiedPathName(rootType, chain);

        // An element of a BufferRef is an addressable place, so the variant is read and written in
        // place through a pointer cast. Anything else is a value, where reinterpret<> yields a copy
        // that can be read but not assigned through.
        if (TryGetAddress(current.Instance, out var address))
        {
            var payloads = string.Concat(Enumerable.Repeat(".payload", chain.Count - 1));
            return $"(({structName}*)({address}))->payload{payloads}";
        }

        if (IsWrittenThrough(field))
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnionVariantWrite, field.Syntax.GetLocation(),
                field.Field.Name));

        var root = current.Instance is IInstanceReferenceOperation or null ? "this" : LowerExpr(current.Instance);
        return $"reinterpret<{structName}>({root}){string.Concat(Enumerable.Repeat(".payload", chain.Count))}";
    }

    // The addressable places: an element of a BufferRef (its ref indexer) and a ref local, which
    // is itself a pointer to one. Nothing else has an address a shader can take.
    private bool TryGetAddress(IOperation? operation, out string address)
    {
        switch (operation)
        {
            case IPropertyReferenceOperation { Instance: { } buffer } indexer when IsBufferRefIndexer(indexer.Property):
                address = $"{LowerExpr(buffer)} + {LowerExpr(indexer.Arguments[0].Value)}";
                return true;
            case ILocalReferenceOperation { Local.RefKind: RefKind.Ref } refLocal:
                address = Naming.ToSlangIdentifier(refLocal.Local.Name);
                return true;
            default:
                address = "";
                return false;
        }
    }

    private static bool IsWrittenThrough(IOperation operation)
    {
        for (var current = operation; current is not null; current = current.Parent)
        {
            switch (current.Parent)
            {
                case ISimpleAssignmentOperation assignment when assignment.Target == current:
                case ICompoundAssignmentOperation compound when compound.Target == current:
                case IIncrementOrDecrementOperation incDec when incDec.Target == current:
                    return true;
                case IFieldReferenceOperation parentField when parentField.Instance == current:
                    continue;
                default:
                    return false;
            }
        }

        return false;
    }

    private string LowerFieldReference(IFieldReferenceOperation field)
    {
        if (UnionLayout.IsVariant(field.Field)) return LowerVariantAccess(field);

        var name = Naming.ToSlangIdentifier(field.Field.Name);

        // Inside a struct's own constructor or method the field is reached through implicit this. C#
        // tells `IndexCount` and a parameter `indexCount` apart by case, but both lower to `indexCount`
        // in Slang, where the parameter would win and `indexCount = indexCount` would assign it to
        // itself. A shader class's fields are module-scope globals, not members, so they stay bare.
        if (field.Instance is IInstanceReferenceOperation)
        {
            if (UnionLayout.IsUnion(field.Field.ContainingType)) return $"this.header.{name}";
            return field.Field.ContainingType.TypeKind == TypeKind.Struct ? $"this.{name}" : name;
        }

        if (field.Instance is null)
        {
            // A [ShaderBinding] field lowers to a module-scope declaration (BindingLowering), not a
            // struct member - referenced by its bare name, same as any other global.
            if (BindingLowering.HasBindingGroupAttribute(field.Field)) return name;

            // An enum member is declared in the emitted Slang enum, so it's referenced the same way
            // C# does - qualified with the type name. Slang enum members keep their C# casing
            // (`enum ResourceType { Texture, ... }`), unlike struct fields, which get lowerCamelCased.
            if (field.Field.ContainingType.TypeKind == TypeKind.Enum)
                return $"{TypeMapping.MapType(field.Field.ContainingType)}.{field.Field.Name}";

            // A plain static/const field (e.g. a private bit-mask/shift constant) has no Slang
            // declaration of its own - StructLowering only emits instance fields - so a qualified
            // reference to it would point at nothing. Inline its compile-time value instead.
            if (field.Field.HasConstantValue)
                return Convert.ToString(field.Field.ConstantValue, CultureInfo.InvariantCulture) ?? "0";

            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                field.Syntax.GetLocation(),
                $"static field '{field.Field.Name}' has no compile-time constant value and no [ShaderBinding]"));
            return "/* unsupported */";
        }

        if (UnionLayout.IsUnion(field.Field.ContainingType))
        {
            // A union value is its Complete struct, whose header is a member; a variant-chain result
            // (foo.Bar.Tag) is already the nested union's header-only struct.
            var header = field.Instance is IFieldReferenceOperation { Field: var parentField } &&
                         UnionLayout.IsVariant(parentField)
                ? ""
                : ".header";
            return $"{LowerExpr(field.Instance)}{header}.{name}";
        }

        return $"{LowerAtLeast(field.Instance, PostfixPrecedence)}.{name}";
    }

    private string LowerInvocation(IInvocationOperation invocation)
    {
        var method = overrides is null
            ? invocation.TargetMethod
            : OverrideResolution.Resolve(invocation.TargetMethod, overrides);

        if (IsVectorMatrixTransform(method))
        {
            var transformArguments = InParameterOrder(invocation.Arguments);
            var v = LowerExpr(transformArguments[0].Value);
            var m = LowerExpr(transformArguments[1].Value);
            return $"mul({v}, {m})";
        }

        var template = IntrinsicBindings.GetSlangExpressionTemplate(method);
        if (template is not null)
        {
            CheckFieldShadowing(TemplateCalleeIdentifier(template), invocation.Syntax.GetLocation());
            return SubstituteTemplate(template, invocation);
        }

        // this-instance is implicit (a shader class calling one of its own instance methods) -
        // emitted as a plain function call, same as a static helper; a real receiver value (e.g. a
        // struct instance method) is prefixed with its lowered expression.
        var instance = invocation.Instance switch
        {
            null or IInstanceReferenceOperation => null,
            var expr => LowerAtLeast(expr, PostfixPrecedence)
        };
        var arguments = InParameterOrder(invocation.Arguments).Select(a => LowerExpr(a.Value)).ToArray();

        if (MethodSource.HasBody(method))
        {
            var calleeName = Naming.ToSlangMethodName(method);
            if (instance is null) CheckFieldShadowing(calleeName, invocation.Syntax.GetLocation());
            var call = $"{calleeName}({string.Join(", ", arguments)})";
            return instance is not null ? $"{instance}.{call}" : call;
        }

        diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.NoSourceForMethod,
            invocation.Syntax.GetLocation(), method.Name));
        return "/* unresolved call */";
    }

    // A computed property (backed by a real getter body, not BufferRef's indexer or a swizzle -
    // both handled above) has no Slang equivalent syntax, so it's read by calling its getter as an
    // ordinary zero-arg method - the getter itself is discovered and emitted the same way any other
    // reachable method is (FunctionCollector's IPropertyReferenceOperation handling).
    // An auto property is its backing field, written directly. A property with a setter body is a
    // call of that setter, emitted as a mutating method like any other struct method.
    private string LowerPropertySet(IPropertyReferenceOperation property, IOperation value)
    {
        var instance = property.Instance switch
        {
            null or IInstanceReferenceOperation => null,
            var expr => LowerAtLeast(expr, PostfixPrecedence)
        };

        if (StructMembers.IsAutoProperty(property.Property))
        {
            var fieldName = Naming.ToSlangIdentifier(property.Property.Name);
            var target = instance is not null ? $"{instance}.{fieldName}"
                : property.Property.ContainingType.TypeKind == TypeKind.Struct ? $"this.{fieldName}" : fieldName;
            return $"{target} = {LowerExpr(value)}";
        }

        if (property.Property.SetMethod is not { } setMethod || !MethodSource.HasBody(setMethod))
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                property.Syntax.GetLocation(), $"assignment to property '{property.Property.Name}' with no setter body"));
            return "/* unsupported */";
        }

        var calleeName = Naming.ToSlangMethodName(setMethod);
        if (instance is null) CheckFieldShadowing(calleeName, property.Syntax.GetLocation());
        var call = $"{calleeName}({LowerExpr(value)})";
        return instance is not null ? $"{instance}.{call}" : call;
    }

    private string LowerPropertyGet(IPropertyReferenceOperation property, IMethodSymbol getMethod)
    {
        var instance = property.Instance switch
        {
            null or IInstanceReferenceOperation => null,
            var expr => LowerAtLeast(expr, PostfixPrecedence)
        };
        var calleeName = Naming.ToSlangMethodName(getMethod);
        if (instance is null) CheckFieldShadowing(calleeName, property.Syntax.GetLocation());
        var call = $"{calleeName}()";
        return instance is not null ? $"{instance}.{call}" : call;
    }

    // An unqualified call whose name matches a field on the struct it's declared in resolves to
    // the field instead in Slang (confirmed against the real compiler) - only checked when
    // shadowableFieldNames is set, i.e. while lowering that struct's own instance method.
    private void CheckFieldShadowing(string calleeIdentifier, Location location)
    {
        if (shadowableFieldNames?.Contains(calleeIdentifier) == true)
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.FieldShadowsCalledFunction,
                location, calleeIdentifier));
    }

    private static string TemplateCalleeIdentifier(string template)
    {
        var end = 0;
        while (end < template.Length && (char.IsLetterOrDigit(template[end]) || template[end] == '_')) end++;
        return template[..end];
    }

    // Slang constructs a value with the type name as a call, not `new` - `Bounds3D(loc)`, not
    // `new Bounds3D(loc)`. The constructor itself (a struct's __init) is discovered and emitted
    // like any other reachable method - see FunctionCollector's IObjectCreationOperation handling.
    private string LowerObjectCreation(IObjectCreationOperation creation)
    {
        if (creation.Constructor is not { } ctor || creation.Type is not { } type)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                creation.Syntax.GetLocation(), "object creation with no explicit constructor"));
            return "/* unsupported */";
        }

        if (!MethodSource.HasBody(ctor) && !IntrinsicBindings.HasBinding(ctor) &&
            !TypeMapping.IsIntrinsicVectorOrMatrixConstructor(type))
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.NoSourceForMethod,
                creation.Syntax.GetLocation(), $"{type.Name} constructor"));
            return "/* unresolved constructor */";
        }

        var arguments = InParameterOrder(creation.Arguments).Select(a => LowerExpr(a.Value)).ToArray();
        return $"{TypeMapping.MapType(type)}({string.Join(", ", arguments)})";
    }

    // The matching helper was collected up front by WithLowering; diagnostic fallback is defensive.
    private string LowerWith(IWithOperation withOperation)
    {
        if (withOperation.Type is null || withOperation.Initializer is null)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                withOperation.Syntax.GetLocation(), "with expression"));
            return "/* unsupported */";
        }

        var overrides = new List<(string SlangName, string ValueExpr)>();
        foreach (var initializer in withOperation.Initializer.Initializers)
        {
            if (initializer is not ISimpleAssignmentOperation { Target: IFieldReferenceOperation fieldRef } assignment)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    initializer.Syntax.GetLocation(), "non-field member in a 'with' initializer"));
                return "/* unsupported */";
            }

            overrides.Add((Naming.ToSlangIdentifier(fieldRef.Field.Name), LowerExpr(assignment.Value)));
        }

        overrides.Sort((a, b) => string.CompareOrdinal(a.SlangName, b.SlangName));

        var key = WithLowering.ComputeKey(withOperation.Type, overrides.Select(o => o.SlangName));
        if (withHelpers is null || !withHelpers.TryGetValue(key, out var spec))
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                withOperation.Syntax.GetLocation(), "with expression (no matching helper collected)"));
            return "/* unsupported */";
        }

        var operand = LowerExpr(withOperation.Operand);
        var arguments = string.Join(", ", overrides.Select(o => o.ValueExpr));
        return $"{spec.FunctionName}({operand}, {arguments})";
    }

    private string SubstituteTemplate(string template, IInvocationOperation invocation)
    {
        var arguments = InParameterOrder(invocation.Arguments).Select(a => a.Value).ToArray();
        var typeArguments = invocation.TargetMethod.TypeArguments;

        // One pass over the placeholders: replacing `@1` with a plain string replace would also hit the
        // start of `@10`, and a substituted argument must never be re-scanned for placeholders.
        return Regex.Replace(template, @"@(this|T\d+|\d+|[A-Za-z_]\w*)", match =>
        {
            var name = match.Groups[1].Value;

            if (name == "this")
                return invocation.Instance is not null and not IInstanceReferenceOperation
                    ? LowerTemplateOperand(invocation.Instance, template, match)
                    : match.Value;

            if (name[0] == 'T' && name.Length > 1 && char.IsDigit(name[1]))
                return int.Parse(name[1..]) < typeArguments.Length
                    ? TypeMapping.MapType(typeArguments[int.Parse(name[1..])])
                    : match.Value;

            if (char.IsDigit(name[0]))
            {
                var index = int.Parse(name);
                return index < arguments.Length ? LowerTemplateOperand(arguments[index], template, match) : match.Value;
            }

            var named = invocation.Arguments.FirstOrDefault(a => a.Parameter?.Name == name);
            if (named is not null) return LowerTemplateOperand(named.Value, template, match);

            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnknownTemplateParameter,
                invocation.Syntax.GetLocation(), match.Value, invocation.TargetMethod.Name));
            return match.Value;
        });
    }

    // An operand sitting between delimiters (`abs(@0)`, `clamp(@0, @1, @2)`) needs no grouping; one that
    // touches an operator or a postfix (`@0[@1]`, `@0.x`) does if it binds looser than a postfix operation.
    private string LowerTemplateOperand(IOperation operand, string template, Match placeholder)
    {
        var text = LowerExpr(operand);

        var before = placeholder.Index == 0 ? '(' : template[placeholder.Index - 1];
        var afterIndex = placeholder.Index + placeholder.Length;
        var after = afterIndex >= template.Length ? ')' : template[afterIndex];
        var delimitedBefore = before is '(' or ',' or ' ' && (before != ' ' || placeholder.Index < 2 || template[placeholder.Index - 2] == ',');
        var delimitedAfter = after is ')' or ',';

        return delimitedBefore && delimitedAfter || Precedence(operand) >= PostfixPrecedence ? text : $"({text})";
    }

    // C#'s operation tree carries no parentheses - `-(a + b)` and `-a + b` are both just a unary over a
    // binary - so a lowered operand is wrapped whenever it binds looser than the operator it sits under.
    // A right operand of the same precedence is wrapped too (`a - (b - c)`), which also keeps the
    // evaluation order of `a + (b + c)`.
    private const int TernaryPrecedence = 1;
    private const int UnaryPrecedence = 12;
    private const int PostfixPrecedence = 100;

    private static int BinaryPrecedence(BinaryOperatorKind kind) => kind switch
    {
        BinaryOperatorKind.Multiply or BinaryOperatorKind.Divide or BinaryOperatorKind.Remainder => 11,
        BinaryOperatorKind.Add or BinaryOperatorKind.Subtract => 10,
        BinaryOperatorKind.LeftShift or BinaryOperatorKind.RightShift => 9,
        BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual or BinaryOperatorKind.GreaterThan
            or BinaryOperatorKind.GreaterThanOrEqual => 8,
        BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals => 7,
        BinaryOperatorKind.And => 6,
        BinaryOperatorKind.ExclusiveOr => 5,
        BinaryOperatorKind.Or => 4,
        BinaryOperatorKind.ConditionalAnd => 3,
        BinaryOperatorKind.ConditionalOr => 2,
        _ => 2
    };

    private static int Precedence(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } implicitConversion)
            operation = implicitConversion.Operand;

        return operation switch
        {
            IBinaryOperation binary when IsMatrixMultiply(binary) => PostfixPrecedence,
            IBinaryOperation binary => BinaryPrecedence(binary.OperatorKind),
            IUnaryOperation or IConversionOperation => UnaryPrecedence,
            IConditionalOperation { WhenFalse: not null } => TernaryPrecedence,
            ISimpleAssignmentOperation or ICompoundAssignmentOperation => 0,
            _ => PostfixPrecedence
        };
    }

    private string LowerAtLeast(IOperation operand, int minimumPrecedence)
    {
        var text = LowerExpr(operand);
        return Precedence(operand) < minimumPrecedence ? $"({text})" : text;
    }

    private static string AsFloatLiteral(string text) =>
        text.Length > 0 && text.TrimStart('-').All(char.IsDigit) ? text + ".0" : text;

    private string LowerBinaryOperand(IOperation operand, BinaryOperatorKind parent, bool isRight)
    {
        var text = LowerExpr(operand);
        var parentPrecedence = BinaryPrecedence(parent);
        var operandPrecedence = Precedence(operand);
        return isRight ? operandPrecedence <= parentPrecedence ? $"({text})" : text
            : operandPrecedence < parentPrecedence ? $"({text})" : text;
    }

    private static bool TryMapOperator(BinaryOperatorKind kind, out string text)
    {
        text = kind switch
        {
            BinaryOperatorKind.Add => "+",
            BinaryOperatorKind.Subtract => "-",
            BinaryOperatorKind.Multiply => "*",
            BinaryOperatorKind.Divide => "/",
            BinaryOperatorKind.Remainder => "%",
            BinaryOperatorKind.Equals => "==",
            BinaryOperatorKind.NotEquals => "!=",
            BinaryOperatorKind.LessThan => "<",
            BinaryOperatorKind.LessThanOrEqual => "<=",
            BinaryOperatorKind.GreaterThan => ">",
            BinaryOperatorKind.GreaterThanOrEqual => ">=",
            BinaryOperatorKind.ConditionalAnd => "&&",
            BinaryOperatorKind.ConditionalOr => "||",
            BinaryOperatorKind.And => "&",
            BinaryOperatorKind.Or => "|",
            BinaryOperatorKind.ExclusiveOr => "^",
            BinaryOperatorKind.LeftShift => "<<",
            BinaryOperatorKind.RightShift => ">>",
            _ => ""
        };
        return text.Length > 0;
    }

    private static bool TryMapUnaryOperator(UnaryOperatorKind kind, out string text)
    {
        text = kind switch
        {
            UnaryOperatorKind.Minus => "-",
            UnaryOperatorKind.Plus => "+",
            UnaryOperatorKind.Not => "!",
            UnaryOperatorKind.BitwiseNegation => "~",
            _ => ""
        };
        return text.Length > 0;
    }

    internal static bool IsBufferRefIndexer(IPropertySymbol property) =>
        property.IsIndexer && property.ContainingType is { Name: "BufferRef" };

    internal static bool IsSwizzle(IPropertySymbol property) =>
        property.ContainingType.TypeKind == TypeKind.Extension &&
        property.Name.Length is >= 1 and <= 4 &&
        property.Name.All(c => "xyzwrgba".Contains(char.ToLowerInvariant(c)));

    // C# has no `*` operator between a vector and Matrix4x4 (System.Numerics uses a static
    // Transform(v, m) method instead) - so unlike matrix*matrix, this is an invocation, not a
    // binary operator, and needs its own recognition.
    private static bool IsVectorMatrixTransform(IMethodSymbol method) =>
        method.Name == "Transform" &&
        method.ContainingType.OriginalDefinition.ToDisplayString() is
            "System.Numerics.Vector2" or "System.Numerics.Vector3" or "System.Numerics.Vector4" &&
        method.Parameters.Length == 2 &&
        method.Parameters[1].Type.OriginalDefinition.ToDisplayString() == "System.Numerics.Matrix4x4";

    private static bool IsMatrixMultiply(IBinaryOperation binary) =>
        binary.OperatorKind == BinaryOperatorKind.Multiply &&
        IsMatrix4X4(binary.LeftOperand.Type) && IsMatrix4X4(binary.RightOperand.Type);

    private static bool IsMatrix4X4(ITypeSymbol? type) =>
        type?.OriginalDefinition.ToDisplayString() == "System.Numerics.Matrix4x4";
}
