namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangSession : IDisposable
{
    private readonly unsafe void * _session;

    public unsafe SlangSession(void * session)
    {
        _session = session;
    }


    SlangResult Compile(string moduleName, string modulePath, string data, string entry)
    {
        unsafe
        {
            return new SlangResult(NativeMethods.SlangSessionCompile(_session, moduleName, modulePath, data, entry));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangSessionDestroy(_session);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SlangSession()
    {
        ReleaseUnmanagedResources();
    }
}