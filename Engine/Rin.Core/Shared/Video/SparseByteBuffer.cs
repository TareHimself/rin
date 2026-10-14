namespace Rin.Core.Shared.Video;

/// <summary>
///     Holds pieces of a file that may have arrived in any order, so a source can hand the decoder what it has and
///     fetch only what it is missing. Pieces that touch are merged, and an append onto the last piece grows it in place.
///     Not thread-safe.
/// </summary>
public sealed class SparseByteBuffer
{
    private readonly List<Piece> _pieces = [];

    /// <summary>
    ///     Where the decoder last said it would not read below again.
    /// </summary>
    public long Released { get; private set; }

    /// <summary>
    ///     End of the run of held bytes that starts at <see cref="Released" />, or <see cref="Released" /> itself.
    /// </summary>
    public long Available => AvailableFrom(Released);

    public long HeldBytes
    {
        get
        {
            var total = 0L;
            foreach (var piece in _pieces) total += piece.Length;
            return total;
        }
    }

    public long AvailableFrom(long offset)
    {
        var index = IndexContaining(offset);
        return index < 0 ? offset : _pieces[index].End;
    }

    public void Read(long offset, Span<byte> destination)
    {
        var index = IndexContaining(offset);
        if (index < 0 || _pieces[index].End < offset + destination.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), $"Bytes [{offset}, {offset + destination.Length}) are not held");

        var piece = _pieces[index];
        piece.Data.AsSpan((int)(offset - piece.Start), destination.Length).CopyTo(destination);
    }

    public void Write(long offset, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;

        var index = 0;
        while (index < _pieces.Count && _pieces[index].End < offset) index++;

        if (index == _pieces.Count || _pieces[index].Start > offset)
        {
            _pieces.Insert(index, new Piece(offset, data));
        }
        else
        {
            _pieces[index].Overwrite(offset, data);
        }

        AbsorbFollowing(index);
    }

    /// <summary>
    ///     Adds the parts of [start, end) that are not held to missing, in order.
    /// </summary>
    public void CollectMissing(long start, long end, List<ByteRange> missing)
    {
        var cursor = start;
        foreach (var piece in _pieces)
        {
            if (piece.End <= cursor) continue;
            if (piece.Start >= end) break;
            if (piece.Start > cursor) missing.Add(new ByteRange(cursor, piece.Start));
            cursor = piece.End;
        }

        if (cursor < end) missing.Add(new ByteRange(cursor, end));
    }

    public void ReleaseBefore(long offset)
    {
        if (offset <= Released) return;
        Released = offset;

        while (_pieces.Count > 0 && _pieces[0].End <= offset) _pieces.RemoveAt(0);
        if (_pieces.Count > 0 && _pieces[0].Start < offset) _pieces[0].TrimStart(offset);
    }

    private int IndexContaining(long offset)
    {
        for (var i = 0; i < _pieces.Count; i++)
        {
            var piece = _pieces[i];
            if (piece.Start > offset) return -1;
            if (offset < piece.End) return i;
        }

        return -1;
    }

    private void AbsorbFollowing(int index)
    {
        var piece = _pieces[index];
        while (index + 1 < _pieces.Count && _pieces[index + 1].Start <= piece.End)
        {
            var next = _pieces[index + 1];
            if (next.End > piece.End) piece.Overwrite(piece.End, next.Data.AsSpan((int)(piece.End - next.Start), (int)(next.End - piece.End)));
            _pieces.RemoveAt(index + 1);
        }
    }

    private sealed class Piece
    {
        public byte[] Data;
        public int Length;
        public long Start;

        public Piece(long start, ReadOnlySpan<byte> data)
        {
            Start = start;
            Data = data.ToArray();
            Length = data.Length;
        }

        public long End => Start + Length;

        public void Overwrite(long offset, ReadOnlySpan<byte> data)
        {
            var relative = (int)(offset - Start);
            var required = relative + data.Length;
            if (required > Data.Length) Array.Resize(ref Data, System.Math.Max(required, Data.Length * 2));
            data.CopyTo(Data.AsSpan(relative));
            Length = System.Math.Max(Length, required);
        }

        // Only moves the rest down once half the piece can go, so repeated small releases stay cheap.
        public void TrimStart(long offset)
        {
            var drop = (int)(offset - Start);
            if (drop < Length / 2) return;
            Array.Copy(Data, drop, Data, 0, Length - drop);
            Start = offset;
            Length -= drop;
        }
    }
}
