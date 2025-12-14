using System.Net;
using System.Net.Http.Headers;
using Rin.Framework.Views;

namespace Rin.Framework.Video;

public class HttpVideoSource : IVideoSource
{
    private readonly HttpClient _client;
    private readonly Uri _uri;
    private bool _hasFetchedHeaders = false;
    private ulong _length;
    private MemoryStream _stream;
    private void FetchHeaders()
    {
        if (_hasFetchedHeaders) return;
        using var resp = _client.Send(new HttpRequestMessage(HttpMethod.Head, _uri));
        _length = ulong.Parse(resp.Content.Headers.GetValues("Content-Length").First());
        _stream = new MemoryStream();
        _hasFetchedHeaders = true;
    }

    public HttpVideoSource(Uri url, HttpRequestHeaders? headers = null)
    {
        _uri = url;
        _client = new HttpClient()
        {
            BaseAddress = url
        };

        if (headers is not null)
        {
            foreach (var header in headers)
            {
                _client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public ulong Length
    {
        get
        {
            FetchHeaders();
            return _length;
        }
    }

    public ulong Available
    {
        get
        {
            FetchHeaders();
            return _length; //(ulong)_stream.Length;
        }
    }

    public void Read(ulong offset, Span<byte> destination)
    {
        FetchHeaders();
        var end = (long)offset + destination.Length;
        var networkStart = _stream.Length;
        if (end > networkStart)
        {
            var preloadAmmount = 1024 * 10;
            if (end - networkStart < preloadAmmount)
            {
                end = (long)offset + preloadAmmount;
            }
            var result = _client.Send(new HttpRequestMessage(HttpMethod.Get, _uri)
            {
                Headers =
                {
                    Range = new RangeHeaderValue(_stream.Length,end - 1)
                }
            });
            if (result.StatusCode == HttpStatusCode.PartialContent)
            {
                var val = result.Content.Headers
                    .GetValues("Content-Range")
                    .First()
                    .Split(' ',StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1)
                    .First()
                    .Split('/',StringSplitOptions.RemoveEmptyEntries)
                    .First()
                    .Split('-',StringSplitOptions.RemoveEmptyEntries);
                _stream.Position = long.Parse(val.First());
            }
            else
            {
                _stream.Position = 0;
            }
            result.Content.ReadAsStream().CopyTo(_stream);
        }
        
        _stream.Position = (long)offset;
        _stream.ReadExactly(destination);
    }
}