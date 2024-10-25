namespace rsl;

public static class Extensions
{
    public static IEnumerable<string> SplitLines(this string str)
    {
        using var reader = new StringReader(str);
        var line = reader.ReadLine();
        while (line != null)
        {
            yield return line;
            line = reader.ReadLine();
        }
    }

    public static Queue<T> ToQueue<T>(this IEnumerable<T> source)
    {
        var q = new Queue<T>();
        foreach (var data in source) q.Enqueue(data);

        return q;
    }
}