using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// A lowered function. A method of a struct is an `extension` member (a static one stays static, so it never
/// collides with a Slang builtin such as `all`): <see cref="ExtensionOf"/> names the struct and
/// <see cref="Writer"/> holds just the member, so neighbouring members of one struct can share a single
/// extension block. Anything else is a free function, printed as is.
/// </summary>
internal readonly record struct LoweredFunction(INamedTypeSymbol? ExtensionOf, SlangWriter? Writer);

/// <summary>
/// Lowers a C# method, constructor or local function to a Slang function.
/// </summary>
internal static class FunctionLowering
{
    /// <summary>
    /// Lowers the method, or reports a diagnostic and returns a default value if it uses a construct that
    /// cannot be emitted (a ref return, an open generic method, an extension of a type Slang cannot extend).
    /// </summary>
    public static LoweredFunction Lower(Compilation compilation, IMethodSymbol method, List<Diagnostic> diagnostics,
        IReadOnlyDictionary<string, WithHelperSpec>? withHelpers = null,
        IReadOnlyDictionary<IMethodSymbol, IMethodSymbol>? overrides = null)
    {
        if (method.RefKind != RefKind.None)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                method.Locations.FirstOrDefault() ?? Location.None, $"ref return on '{method.Name}'"));
            return default;
        }

        if (method.TypeArguments.Any(argument => argument is ITypeParameterSymbol))
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                method.Locations.FirstOrDefault() ?? Location.None, $"open generic method '{method.Name}'"));
            return default;
        }

        var isConstructor = method.MethodKind == MethodKind.Constructor;
        var signature = isConstructor
            ? "__init"
            : $"{TypeMapping.MapType(method.ReturnType)} {Naming.ToSlangMethodName(method)}";

        var writer = new SlangWriter();

        if (ExtensionMembers.TryGetReceiver(method, out var receiver))
            return LowerExtensionMember(compilation, method, receiver, diagnostics, writer, signature, withHelpers,
                overrides);

        if (method.ContainingType.TypeKind == TypeKind.Struct && method.IsStatic)
        {
            WriteSignatureAndBody(compilation, method, diagnostics, writer, $"static {signature}",
                withHelpers: withHelpers, overrides: overrides);
            return new LoweredFunction(method.ContainingType, writer);
        }

        if (method.ContainingType.TypeKind == TypeKind.Struct)
        {
            // An extension member behaves like one declared in the struct, and call sites look the same
            // either way. Constructors never need [mutating]; other methods get it only when the body
            // writes through the implicit `this`, since over-marking a read-only method is unverified.
            var fieldNames = method.ContainingType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => !f.IsStatic && !f.IsImplicitlyDeclared)
                .Select(f => Naming.ToSlangIdentifier(f.Name))
                .ToHashSet();

            writer.Origin = method.Locations.FirstOrDefault();
            if (!isConstructor && WritesToImplicitThis(compilation, method)) writer.Line("[mutating]");
            WriteSignatureAndBody(compilation, method, diagnostics, writer, signature, fieldNames, withHelpers,
                overrides);
            return new LoweredFunction(method.ContainingType, writer);
        }

        WriteSignatureAndBody(compilation, method, diagnostics, writer, signature, withHelpers: withHelpers,
            overrides: overrides);
        return new LoweredFunction(null, writer);
    }

    // An extension member joins the Slang `extension` block of its receiver type, where the receiver is `this`.
    // A by-value receiver the body writes to is copied into a local first, since a Slang [mutating] member
    // would write through to the caller's variable; a `ref` receiver is meant to, so it becomes [mutating].
    private static LoweredFunction LowerExtensionMember(Compilation compilation, IMethodSymbol method,
        ExtensionReceiver receiver, List<Diagnostic> diagnostics, SlangWriter writer, string signature,
        IReadOnlyDictionary<string, WithHelperSpec>? withHelpers,
        IReadOnlyDictionary<IMethodSymbol, IMethodSymbol>? overrides)
    {
        if (receiver.Type is not INamedTypeSymbol receiverType || !TypeMapping.IsLegalShaderType(receiverType) ||
            receiverType is { Name: "BufferRef" })
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.ExtensionMethodNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, method.Name, receiver.Type.ToDisplayString()));
            return default;
        }

        var parameters = receiver.IsClassic ? method.Parameters.Skip(1) : method.Parameters;
        var fieldNames = receiverType.TypeKind == TypeKind.Struct
            ? receiverType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => !f.IsStatic && !f.IsImplicitlyDeclared)
                .Select(f => Naming.ToSlangIdentifier(f.Name))
                .ToHashSet()
            : null;

        writer.Origin = method.Locations.FirstOrDefault();
        if (receiver.Parameter is null)
        {
            WriteSignatureAndBody(compilation, method, diagnostics, writer, $"static {signature}", fieldNames,
                withHelpers, overrides, parameters);
            return new LoweredFunction(receiverType, writer);
        }

        var writes = ExtensionMembers.WritesToReceiver(compilation, method, receiver.Parameter);
        var byReference = receiver.Parameter.RefKind == RefKind.Ref;
        if (writes && byReference) writer.Line("[mutating]");
        WriteSignatureAndBody(compilation, method, diagnostics, writer, signature, fieldNames, withHelpers, overrides,
            parameters, new ReceiverLowering(receiver.Parameter, writes && !byReference));
        return new LoweredFunction(receiverType, writer);
    }

    /// <summary>
    /// Writes <c>signaturePrefix(parameters)</c> followed by the braced lowered body.
    /// </summary>
    public static void WriteSignatureAndBody(Compilation compilation, IMethodSymbol method,
        List<Diagnostic> diagnostics, SlangWriter writer, string signaturePrefix,
        IReadOnlySet<string>? shadowableFieldNames = null,
        IReadOnlyDictionary<string, WithHelperSpec>? withHelpers = null,
        IReadOnlyDictionary<IMethodSymbol, IMethodSymbol>? overrides = null,
        IEnumerable<IParameterSymbol>? parameters = null, ReceiverLowering? receiver = null)
    {
        parameters ??= method.Parameters;
        foreach (var arrayType in method.Parameters.Select(p => p.Type).Append(method.ReturnType)
                     .OfType<IArrayTypeSymbol>())
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedType,
                method.Locations.FirstOrDefault() ?? Location.None,
                $"{arrayType.ToDisplayString()} on '{method.Name}' (declare an [InlineArray] struct instead)"));

        foreach (var parameter in method.Parameters.Where(p => TypeMapping.IsDouble(p.Type)))
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.DoubleNotSupported,
                parameter.Locations.FirstOrDefault() ?? Location.None, $"{method.Name}({parameter.Name})"));
        if (TypeMapping.IsDouble(method.ReturnType))
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.DoubleNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, $"{method.Name} return type"));

        var parameterList = string.Join(", ", parameters.Select(p =>
            $"{RefModifier(p.RefKind)}{TypeMapping.MapType(p.Type)} {Naming.ToSlangIdentifier(p.Name)}"));

        writer.Origin = method.Locations.FirstOrDefault();
        writer.OpenBrace($"{signaturePrefix}({parameterList})");

        if (receiver is { AsLocal: true })
            writer.Line($"{TypeMapping.MapType(receiver.Parameter.Type)} {Naming.ToSlangIdentifier(receiver.Parameter.Name)} = this;");

        var body = new BodyLowering(diagnostics, writer, shadowableFieldNames, withHelpers, overrides,
            TypeSubstitution.For(method), receiver);
        foreach (var bodyOperation in MethodSource.GetBodies(method, compilation))
            body.LowerStatement(bodyOperation);

        writer.CloseBrace();
        writer.Origin = null;
    }

    private static bool WritesToImplicitThis(Compilation compilation, IMethodSymbol method)
    {
        foreach (var body in MethodSource.GetBodies(method, compilation))
        foreach (var op in body.DescendantsAndSelf())
            if (op is ISimpleAssignmentOperation { Target: var target } && RootedAtImplicitThis(target))
                return true;
        return false;
    }

    // `_locationU.X = v` (field of a field) and `Location = v` (setter) mutate the struct just like `Field = v`.
    private static bool RootedAtImplicitThis(IOperation target) => target switch
    {
        IFieldReferenceOperation { Instance: IInstanceReferenceOperation } => true,
        IFieldReferenceOperation { Instance: { } instance } => RootedAtImplicitThis(instance),
        IPropertyReferenceOperation { Instance: IInstanceReferenceOperation } => true,
        _ => false
    };

    private static string RefModifier(RefKind refKind) => refKind switch
    {
        RefKind.Out => "out ",
        RefKind.Ref => "inout ",
        RefKind.In => "in ",
        _ => ""
    };
}
