namespace rin.Framework.Core.Logging;

public class FileTransport : ITransport
{
    private readonly StreamWriter _fileStream;
    private uint _uses = 0;
    private readonly bool _async;
    
    public FileTransport(string filePath,bool async = false)
    {
        _fileStream = new StreamWriter(filePath)
        {
            AutoFlush = true
        };
        _async = async;
    }

    public void Dispose()
    {
        _uses--;
        if (_uses == 0)
        {
            _fileStream.Dispose();
        }
    }

    public void OnAdded(ILogger logger)
    {
        _uses++;
    }

    public void Info(string message)
    {
        if (_async)
        {
            _fileStream.WriteLineAsync(message).ConfigureAwait(false);
        }
        else
        {
            _fileStream.WriteLine(message);
        }
    }

    public void Warn(string message)
    {
        if (_async)
        {
            _fileStream.WriteLineAsync(message).ConfigureAwait(false);
        }
        else
        {
            _fileStream.WriteLine(message);
        }
    }

    public void Error(string message, Exception? exception)
    {
        if (_async)
        {
            _fileStream.WriteLineAsync(message).ConfigureAwait(false);
        }
        else
        {
            _fileStream.WriteLine(message);
        }
    }
}