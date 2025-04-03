using System.Runtime.InteropServices;
using System.Text;

namespace Rin.Engine.Graphics.Shaders.Slang;

public class SlangSessionBuilder : IDisposable
{
    private readonly unsafe void* _ptr;

    public SlangSessionBuilder()
    {
        unsafe
        {
            _ptr = Native.Slang.SessionBuilderNew(&LoadFile);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private static unsafe int LoadFile(byte* path, byte** data)
    {
        var asStringPath = Marshal.PtrToStringUTF8(new IntPtr(path)) ?? string.Empty;
        try
        {
            using var stream = SEngine.Get().Sources.Read(asStringPath);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            var contentBytes = (byte*)Native.Memory.Allocate((uint)Engine.Utils.ByteSizeOf<byte>(content.Length));
            var contentBytesSpan = new Span<byte>(contentBytes, content.Length);
            Encoding.UTF8.GetBytes(content, contentBytesSpan);
            *data = contentBytes;
            return content.Length;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 0;
        }

        return 0;
    }

    public SlangSessionBuilder AddTargetSpirv()
    {
        unsafe
        {
            Native.Slang.SessionBuilderAddTargetSpirv(_ptr);
        }

        return this;
    }

    public SlangSessionBuilder AddTargetGlsl()
    {
        unsafe
        {
            Native.Slang.SessionBuilderAddTargetGlsl(_ptr);
        }

        return this;
    }

    public SlangSessionBuilder AddPreprocessorDefinition(string name, string value)
    {
        unsafe
        {
            Native.Slang.SessionBuilderAddPreprocessorDefinition(_ptr, name, value);
        }

        return this;
    }

    public SlangSessionBuilder AddSearchPath(string searchPath)
    {
        unsafe
        {
            Native.Slang.SessionBuilderAddSearchPath(_ptr, searchPath);
        }

        return this;
    }

    public SlangSession Build()
    {
        unsafe
        {
            return new SlangSession(Native.Slang.SessionBuilderBuild(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.Slang.SessionBuilderFree(_ptr);
        }
    }

    ~SlangSessionBuilder()
    {
        ReleaseUnmanagedResources();
    }
}