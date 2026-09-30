using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Walks a method's body for calls to other plain (non-[SlangCall]-bound) methods with source,
/// post-order, so a callee is always collected before its caller - mirrors TypeCollector's
/// dependency ordering, but for the call graph instead of the field graph. A method never reached
/// this way is never added to Order, which is the dead-code-elimination guarantee. Local functions
/// go through the exact same walk as any other method - MethodSource treats both declaration
/// shapes uniformly. Constructors reached via `new T(...)` are collected the same way as ordinary
/// calls, so a struct's __init gets emitted whenever something actually constructs it.
/// </summary>
internal sealed class FunctionCollector(
    Compilation compilation, List<Diagnostic> diagnostics,
    IReadOnlyDictionary<IMethodSymbol, IMethodSymbol>? overrides = null)
{
    private readonly HashSet<IMethodSymbol> _visited = new(SymbolEqualityComparer.Default);
    private readonly HashSet<IMethodSymbol> _visiting = new(SymbolEqualityComparer.Default);

    public List<IMethodSymbol> Order { get; } = [];

    public void Collect(IMethodSymbol method)
    {
        if (!_visiting.Add(method))
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.RecursionNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, method.Name));
            return;
        }

        foreach (var body in MethodSource.GetBodies(method, compilation))
            foreach (var callSite in FindReachableCallSites(body))
            {
                var target = callSite switch
                {
                    IInvocationOperation invocation => invocation.TargetMethod,
                    IObjectCreationOperation { Constructor: { } ctor } => ctor,
                    IPropertyReferenceOperation { Property.SetMethod: { } setMethod } property
                        when IsAssignmentTarget(property) => setMethod,
                    IPropertyReferenceOperation { Property.GetMethod: { } getMethod } => getMethod,
                    _ => null
                };
                if (target is not null)
                    VisitCallee(overrides is null ? target : OverrideResolution.Resolve(target, overrides));
            }

        _visiting.Remove(method);
        if (_visited.Add(method)) Order.Add(method);
    }

    private void VisitCallee(IMethodSymbol method)
    {
        if (IntrinsicBindings.HasBinding(method)) return;
        if (!MethodSource.HasBody(method)) return;
        if (_visited.Contains(method)) return;

        Collect(method);
    }

    // A plain descendants walk would also find calls inside a nested, never-invoked local
    // function's body, since it's lexically nested in the enclosing method's tree - that would
    // defeat dead-code elimination for local functions. A local function's own body is only ever
    // walked (via Collect, above) once something actually calls it, so descent stops here.
    private static IEnumerable<IOperation> FindReachableCallSites(IOperation root)
    {
        if (root is IInvocationOperation or IObjectCreationOperation) yield return root;

        // A property *read* needs its getter collected like any other call; a property used as an
        // assignment target does not (property writes aren't lowered generically - see BodyLowering),
        // and yielding it here too would collect a getter that's never actually invoked, defeating
        // dead-code elimination for it.
        if (root is IPropertyReferenceOperation property &&
            !BodyLowering.IsBufferRefIndexer(property.Property) &&
            !BodyLowering.IsSwizzle(property.Property))
            yield return root;

        if (root is ILocalFunctionOperation) yield break;

        foreach (var child in root.ChildOperations)
        foreach (var found in FindReachableCallSites(child))
            yield return found;
    }

    private static bool IsAssignmentTarget(IOperation operation) => operation.Parent switch
    {
        ISimpleAssignmentOperation assignment => assignment.Target == operation,
        ICompoundAssignmentOperation compound => compound.Target == operation,
        IIncrementOrDecrementOperation incDec => incDec.Target == operation,
        _ => false
    };
}
