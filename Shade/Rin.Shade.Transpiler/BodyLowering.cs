using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// v1 statement/expression set: block, if/for/while/switch, fixed-size-array foreach, return,
/// local declaration ('var' only), assignment/compound-assignment, field/parameter/local
/// reference, BufferRef&lt;T&gt; indexer, swizzles, binary/unary/ternary operators, matrix mul(),
/// literals, casts, `with` (WithLowering), and [SlangCall]/[SlangStatement] invocations. A local
/// function is inlined. Anything else is a diagnostic, not a crash.
/// </summary>
internal sealed class BodyLowering(
    List<Diagnostic> diagnostics, SlangWriter writer, IReadOnlySet<string>? shadowableFieldNames = null,
    IReadOnlyDictionary<string, WithHelperSpec>? withHelpers = null)
{
    private int _loopDepth;

    public void LowerStatement(IOperation? op)
    {
        switch (op)
        {
            case null:
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
                    // 'var' needs a right-hand side to infer from - an uninitialized declaration
                    // (e.g. a pre-declared 'out' argument) has to spell out its real type instead.
                    writer.Line(initializer is null
                        ? $"{TypeMapping.MapType(declarator.Symbol.Type)} {name};"
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

    private void LowerFixedArrayForEach(IForEachLoopOperation forEach)
    {
        var collection = forEach.Collection is IConversionOperation { IsImplicit: true } conversion
            ? conversion.Operand
            : forEach.Collection;

        if (collection is IFieldReferenceOperation { Field: var field } fieldRef &&
            FixedSizeAttributeReader.GetSize(field) is { } size)
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
            forEach.Syntax.GetLocation(), "foreach (only fixed-size array fields are supported)"));
    }

    private string LowerLoopClause(IOperation op) => op switch
    {
        IVariableDeclarationGroupOperation group => string.Join(", ", group.Declarations
            .SelectMany(d => d.Declarators)
            .Select(d => $"var {Naming.ToSlangIdentifier(d.Symbol.Name)} = {LowerExpr(d.Initializer!.Value)}")),
        IExpressionStatementOperation stmt => LowerExpr(stmt.Operation),
        _ => LowerExpr(op)
    };

    private string LowerExpr(IOperation op)
    {
        switch (op)
        {
            case IParameterReferenceOperation parameter:
                return Naming.ToSlangIdentifier(parameter.Parameter.Name);
            case ILocalReferenceOperation local:
                return Naming.ToSlangIdentifier(local.Local.Name);
            case IIncrementOrDecrementOperation inc:
                return $"{LowerExpr(inc.Target)}{(inc.Kind == OperationKind.Increment ? "++" : "--")}";
            case IFieldReferenceOperation field:
                return LowerFieldReference(field);
            case IPropertyReferenceOperation property when IsBufferRefIndexer(property.Property):
                return $"{LowerExpr(property.Instance!)}[{LowerExpr(property.Arguments[0].Value)}]";
            case IArrayElementReferenceOperation { Indices: [var index] } element:
                return $"{LowerExpr(element.ArrayReference)}[{LowerExpr(index)}]";
            case IPropertyReferenceOperation property when IsSwizzle(property.Property):
                return $"{LowerExpr(property.Instance!)}.{Naming.ToSlangIdentifier(property.Property.Name)}";
            case ILiteralOperation { ConstantValue: { HasValue: true, Value: var value } }:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
            case IConversionOperation conversion:
                return conversion.IsImplicit
                    ? LowerExpr(conversion.Operand)
                    : $"({TypeMapping.MapType(conversion.Type!)}){LowerExpr(conversion.Operand)}";
            case IBinaryOperation binary when IsMatrixMultiply(binary):
                return $"mul({LowerExpr(binary.RightOperand)}, {LowerExpr(binary.LeftOperand)})";
            case IBinaryOperation { OperatorMethod.DeclaringSyntaxReferences.Length: > 0 } binary:
                // A BCL operator (Vector2.op_Subtraction etc.) has no declaring syntax in this
                // compilation and is trusted to mean the same thing in Slang - an operator declared
                // in source here is user-authored and needs its own [SlangCall] binding rather than
                // being silently treated as a passthrough.
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    binary.Syntax.GetLocation(), $"user-defined operator overload '{binary.OperatorMethod!.Name}'"));
                return "/* unsupported */";
            case IBinaryOperation binary when TryMapOperator(binary.OperatorKind, out var operatorText):
                return $"{LowerExpr(binary.LeftOperand)} {operatorText} {LowerExpr(binary.RightOperand)}";
            case IUnaryOperation { OperatorMethod.DeclaringSyntaxReferences.Length: > 0 } unary:
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    unary.Syntax.GetLocation(), $"user-defined operator overload '{unary.OperatorMethod!.Name}'"));
                return "/* unsupported */";
            case IUnaryOperation unary when TryMapUnaryOperator(unary.OperatorKind, out var operatorText):
                return $"{operatorText}{LowerExpr(unary.Operand)}";
            case ICompoundAssignmentOperation { OperatorMethod.DeclaringSyntaxReferences.Length: > 0 } compound:
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    compound.Syntax.GetLocation(),
                    $"user-defined operator overload '{compound.OperatorMethod!.Name}'"));
                return "/* unsupported */";
            case ICompoundAssignmentOperation compound when TryMapOperator(compound.OperatorKind, out var operatorText):
                return $"{LowerExpr(compound.Target)} {operatorText}= {LowerExpr(compound.Value)}";
            case ISimpleAssignmentOperation assignment:
                return $"{LowerExpr(assignment.Target)} = {LowerExpr(assignment.Value)}";
            // Ternary and `if` share IConditionalOperation; LowerStatement handles the if-shape first.
            case IConditionalOperation { WhenFalse: not null } conditional:
                return $"{LowerExpr(conditional.Condition)} ? {LowerExpr(conditional.WhenTrue)} : {LowerExpr(conditional.WhenFalse)}";
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

    private string LowerFieldReference(IFieldReferenceOperation field)
    {
        var name = Naming.ToSlangIdentifier(field.Field.Name);

        if (field.Instance is IInstanceReferenceOperation) return name;

        if (field.Instance is null)
        {
            // A [ShaderBinding] field lowers to a module-scope declaration (BindingLowering), not a
            // struct member - referenced by its bare name, same as any other global.
            if (BindingLowering.HasShaderBindingAttribute(field.Field)) return name;

            // Otherwise a static reference is, in practice, always an enum member. Slang enum
            // members keep their C# casing (`enum ResourceType { Texture, ... }`), unlike struct
            // fields, which get lowerCamelCased - so only qualify with the type name here.
            var typeName = TypeMapping.MapType(field.Field.ContainingType);
            return field.Field.ContainingType.TypeKind == TypeKind.Enum
                ? $"{typeName}.{field.Field.Name}"
                : $"{typeName}.{name}";
        }

        return $"{LowerExpr(field.Instance)}.{name}";
    }

    private string LowerInvocation(IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        if (IsVectorMatrixTransform(method))
        {
            var v = LowerExpr(invocation.Arguments[0].Value);
            var m = LowerExpr(invocation.Arguments[1].Value);
            return $"mul({v}, {m})";
        }

        var template = IntrinsicBindings.GetSlangCallTemplate(method);
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
            var expr => LowerExpr(expr)
        };
        var arguments = invocation.Arguments.Select(a => LowerExpr(a.Value)).ToArray();

        if (MethodSource.HasBody(method))
        {
            var calleeName = Naming.ToSlangIdentifier(method.Name);
            if (instance is null) CheckFieldShadowing(calleeName, invocation.Syntax.GetLocation());
            var call = $"{calleeName}({string.Join(", ", arguments)})";
            return instance is not null ? $"{instance}.{call}" : call;
        }

        diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.NoSourceForMethod,
            invocation.Syntax.GetLocation(), method.Name));
        return "/* unresolved call */";
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

        var arguments = creation.Arguments.Select(a => LowerExpr(a.Value)).ToArray();
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
        var result = template;

        var arguments = invocation.Arguments.Select(a => LowerExpr(a.Value)).ToArray();
        for (var i = 0; i < arguments.Length; i++)
            result = result.Replace($"${i}", arguments[i]);

        var typeArguments = invocation.TargetMethod.TypeArguments;
        for (var i = 0; i < typeArguments.Length; i++)
            result = result.Replace($"$T{i}", TypeMapping.MapType(typeArguments[i]));

        if (invocation.Instance is not null and not IInstanceReferenceOperation)
            result = result.Replace("$this", LowerExpr(invocation.Instance));

        return result;
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

    private static bool IsBufferRefIndexer(IPropertySymbol property) =>
        property.IsIndexer && property.ContainingType is { Name: "BufferRef" };

    private static bool IsSwizzle(IPropertySymbol property) =>
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
