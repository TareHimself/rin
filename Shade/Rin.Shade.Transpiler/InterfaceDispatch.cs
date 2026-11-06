using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Resolves a call to an interface member to the concrete type's implementation. A shader has no runtime
/// dispatch, so this only works once substitution has turned the receiver into a concrete type: `TMaterial.Evaluate`
/// inside `SurfaceShader&lt;Gold&gt;` becomes `Gold.Evaluate`.
/// </summary>
internal static class InterfaceDispatch
{
    public static IMethodSymbol Resolve(IInvocationOperation invocation, IMethodSymbol method,
        TypeSubstitution? substitution)
    {
        if (method.ContainingType?.TypeKind != TypeKind.Interface) return method;

        var receiver = method.IsStatic ? invocation.ConstrainedToType : invocation.Instance?.Type;
        if (receiver is null) return method;

        var concrete = substitution?.Apply(receiver) ?? receiver;
        if (concrete is ITypeParameterSymbol || concrete.TypeKind == TypeKind.Interface) return method;

        return concrete.FindImplementationForInterfaceMember(method) as IMethodSymbol ?? method;
    }
}
