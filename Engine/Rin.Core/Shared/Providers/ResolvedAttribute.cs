namespace Rin.Core.Shared.Providers;

/// <summary>
///     Marks a <c>partial</c> property as generator-backed by <see cref="Global.Provider" />. The generator
///     emits <c>get =&gt; field ??= Global.Provider.Get&lt;T&gt;();</c> for the property, so the provider is
///     only ever consulted the first time the property is read. If the property also declares a setter,
///     assigning it first (as a test would, to inject a fake) short-circuits resolution entirely - the
///     provider is never touched.
/// </summary>
/// <example>
///     <code>
/// public partial class SomeConsumer
/// {
///     [Resolved] public partial IGraphicsModule Graphics { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ResolvedAttribute : Attribute;
