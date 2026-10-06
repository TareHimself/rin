using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Rin.Core.Graphics.Graph;

namespace RenderGraphOverlay.Snapshot;

internal static partial class PassInspector
{
    private const int MaxFields = 12;
    private const int MaxElements = 6;
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [GeneratedRegex(@"^<(?<name>\w+)>(k__BackingField|P)$")]
    private static partial Regex CompilerGeneratedName();

    public static IReadOnlyList<string> DescribeFields(IPass pass)
    {
        var lines = new List<string>();
        foreach (var field in pass.GetType().GetFields(InstanceFields))
        {
            if (lines.Count >= MaxFields) break;
            if (!TryGetDisplayName(field, out var name) || name == nameof(IPass.Id)) continue;

            var value = field.GetValue(pass);
            lines.Add($"{name} = {Format(value)}");
            if (value is Array { Length: > 0 } array && IsDescribable(array.GetValue(0)!))
                lines.AddRange(array.Cast<object>().Take(MaxElements).Select(e => $"    {DescribeElement(e)}"));
        }

        return lines;
    }

    private static bool TryGetDisplayName(FieldInfo field, out string name)
    {
        name = field.Name;
        if (!field.Name.StartsWith('<')) return true;

        var match = CompilerGeneratedName().Match(field.Name);
        if (!match.Success) return false;

        name = match.Groups["name"].Value;
        return true;
    }

    private static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            string text => $"\"{text}\"",
            Delegate => "delegate",
            Array array => $"{array.GetType().GetElementType()!.Name}[{array.Length}]",
            ICollection collection => $"{collection.GetType().Name}({collection.Count})",
            _ when value.GetType().IsPrimitive || value.GetType().IsEnum => value.ToString()!,
            _ => value.GetType().Name
        };
    }

    private static bool IsDescribable(object element)
    {
        return element.GetType().GetProperty("ResourceId") is not null;
    }

    private static string DescribeElement(object element)
    {
        var id = ReadProperty(element, "ResourceId");
        var before = ReadProperty(element, "PreviousLayout") ?? ReadProperty(element, "PreviousUsage");
        var after = ReadProperty(element, "NextLayout") ?? ReadProperty(element, "NextUsage");
        return $"#{id} {before} -> {after}";
    }

    private static string? ReadProperty(object target, string name)
    {
        return target.GetType().GetProperty(name)?.GetValue(target)?.ToString();
    }
}
