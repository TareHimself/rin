using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Walks a method's body for calls to other plain (non-[SlangExpression]-bound) methods with source,
/// post-order, so a callee is always collected before its caller. A method never reached this way is
/// never added to Order, which is what eliminates dead code. Local functions, property accessors and
/// constructors reached via `new T(...)` are collected like ordinary calls.
/// </summary>
internal sealed class FunctionCollector(
    Compilation compilation, List<Diagnostic> diagnostics,
    IReadOnlyDictionary<IMethodSymbol, IMethodSymbol>? overrides = null)
{
    private readonly HashSet<IMethodSymbol> _visited = new(SymbolEqualityComparer.Default);
    private readonly HashSet<IMethodSymbol> _visiting = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// The reachable methods, each after everything it calls.
    /// </summary>
    public List<IMethodSymbol> Order { get; } = [];

    /// <summary>
    /// The collected methods each method calls, so the emitter can reorder without breaking define-before-use.
    /// </summary>
    public Dictionary<IMethodSymbol, HashSet<IMethodSymbol>> Dependencies { get; } = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Adds the method and, first, every method it reaches. Reports a diagnostic if the call graph is recursive.
    /// </summary>
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
                if (target is null) continue;

                var callee = overrides is null ? target : OverrideResolution.Resolve(target, overrides);
                VisitCallee(callee);
                if (!IntrinsicBindings.HasBinding(callee) && MethodSource.HasBody(callee))
                {
                    if (!Dependencies.TryGetValue(method, out var callees))
                        Dependencies[method] = callees = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
                    callees.Add(callee);
                }
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

    // Stops at a local function: its body is lexically nested here but is only walked (via Collect)
    // once something calls it, otherwise dead local functions would keep their callees alive.
    private static IEnumerable<IOperation> FindReachableCallSites(IOperation root)
    {
        if (root is IInvocationOperation or IObjectCreationOperation) yield return root;

        // A property read needs its getter collected. An assignment target is excluded here (its setter
        // is picked up in Collect), so a getter that is never invoked is not kept alive.
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
