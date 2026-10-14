using System.Net;
using System.Net.Http.Headers;

namespace Rin.Core.Shared.Video;

/// <summary>
///     Fetches a webm over HTTP as the decoder asks for it. With range requests a seek fetches only the part of the
///     file it lands on, and bytes the decoder is done with are dropped. Without them the whole file is fetched once.
/// </summary>
public sealed class HttpVideoSource : IVideoSource
{
    private const long MinimumFetchBytes = 256 * 1024;
    private const long UnknownLength = -1;
    private readonly SparseByteBuffer _buffer = new();
    private readonly HttpClient _client;
    private readonly List<ByteRange> _missing = [];
    private readonly Uri _uri;
    private bool _hasWholeFile;
    private long _length = UnknownLength;
    private bool _probed;

    public HttpVideoSource(Uri url, HttpRequestHeaders? headers = null)
    {
        _uri = url;
        _client = new HttpClient();
        if (headers is null) return;
        foreach (var header in headers) _client.DefaultRequestHeaders.Add(header.Key, header.Value);
    }

    public long Length
    {
        get
        {
            Probe();
            return _length;
        }
    }

    public long Available => _buffer.Available;

    public void Read(long offset, Span<byte> destination)
    {
        _buffer.Read(offset, destination);
    }

    public long AvailableFrom(long offset)
    {
        return _buffer.AvailableFrom(offset);
    }

    public void MakeRangeAvailable(long offset, long size)
    {
        Probe();
        if (_hasWholeFile) return;

        var end = offset + System.Math.Max(size, MinimumFetchBytes);
        if (_length != UnknownLength) end = System.Math.Min(end, _length);

        _missing.Clear();
        _buffer.CollectMissing(offset, end, _missing);
        foreach (var range in _missing)
        {
            Fetch(range);
            if (_hasWholeFile) return;
        }
    }

    public void ReleaseBefore(long offset)
    {
        _buffer.ReleaseBefore(offset);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private void Probe()
    {
        if (_probed) return;
        _probed = true;
        using var response = _client.Send(new HttpRequestMessage(HttpMethod.Head, _uri));
        response.EnsureSuccessStatusCode();
        _length = response.Content.Headers.ContentLength ?? UnknownLength;
    }

    private void Fetch(ByteRange range)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _uri);
        request.Headers.Range = new RangeHeaderValue(range.Start, range.End - 1);
        using var response = _client.Send(request, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            _length = response.Content.Headers.ContentRange?.Length ?? _buffer.Available;
            return;
        }

        response.EnsureSuccessStatusCode();

        var isPartial = response.StatusCode == HttpStatusCode.PartialContent;
        var start = isPartial ? response.Content.Headers.ContentRange?.From ?? range.Start : 0;
        using var body = response.Content.ReadAsStream();
        using var memory = new MemoryStream();
        body.CopyTo(memory);
        _buffer.Write(start, memory.GetBuffer().AsSpan(0, (int)memory.Length));

        if (isPartial) return;
        _hasWholeFile = true;
        _length = memory.Length;
    }
}
