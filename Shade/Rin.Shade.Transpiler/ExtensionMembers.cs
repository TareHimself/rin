using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// The receiver of an extension member: its type, the parameter that names it in the body (null for a static
/// member of a C# 14 extension block) and whether the member is a classic `this`-parameter extension method,
/// whose receiver is also its first parameter.
/// </summary>
internal readonly record struct ExtensionReceiver(ITypeSymbol Type, IParameterSymbol? Parameter, bool IsClassic);

/// <summary>
/// How a method body refers to its extension receiver: as Slang's `this`, or through a local copy of it (a
/// by-value receiver the body writes to, which must not write through to the caller's variable).
/// </summary>
internal sealed record ReceiverLowering(IParameterSymbol Parameter, bool AsLocal);

/// <summary>
/// Recognizes C# extension members, both C# 14 `extension(T x) { ... }` blocks and classic `this T x` methods,
/// which lower to members of a Slang `extension T { ... }` block.
/// </summary>
internal static class ExtensionMembers
{
    /// <summary>
    /// The unreduced form of a classic extension method called as `x.Foo()`, so the receiver is Parameters[0] again
    /// and the symbol matches the one its body was bound against.
    /// </summary>
    public static IMethodSymbol Normalize(IMethodSymbol method) =>
        method.ReducedFrom is null ? method : method.GetConstructedReducedFrom() ?? method.ReducedFrom;

    public static bool TryGetReceiver(IMethodSymbol method, out ExtensionReceiver receiver)
    {
        if (method.ContainingType is { TypeKind: TypeKind.Extension, ExtensionParameter: { } parameter })
        {
            receiver = new ExtensionReceiver(parameter.Type, method.IsStatic ? null : parameter, false);
            return true;
        }

        if (method.IsExtensionMethod && method.Parameters.Length > 0)
        {
            receiver = new ExtensionReceiver(method.Parameters[0].Type, method.Parameters[0], true);
            return true;
        }

        receiver = default;
        return false;
    }

    public static bool IsReceiver(IParameterSymbol parameter, IParameterSymbol receiver) =>
        SymbolEqualityComparer.Default.Equals(parameter.OriginalDefinition, receiver.OriginalDefinition);

    /// <summary>
    /// Whether the body assigns to the receiver or to a member of it, or passes it by reference.
    /// </summary>
    public static bool WritesToReceiver(Compilation compilation, IMethodSymbol method, IParameterSymbol receiver)
    {
        foreach (var body in MethodSource.GetBodies(method, compilation))
        foreach (var operation in body.DescendantsAndSelf())
        {
            var target = operation switch
            {
                ISimpleAssignmentOperation assignment => assignment.Target,
                ICompoundAssignmentOperation compound => compound.Target,
                IIncrementOrDecrementOperation incDec => incDec.Target,
                IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out } argument => argument.Value,
                _ => null
            };
            if (target is not null && RootedAtReceiver(target, receiver)) return true;
        }

        return false;
    }

    private static bool RootedAtReceiver(IOperation target, IParameterSymbol receiver) => target switch
    {
        IParameterReferenceOperation parameter => IsReceiver(parameter.Parameter, receiver),
        IFieldReferenceOperation { Instance: { } instance } => RootedAtReceiver(instance, receiver),
        IPropertyReferenceOperation { Instance: { } instance } => RootedAtReceiver(instance, receiver),
        _ => false
    };
}
