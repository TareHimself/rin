using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace rin.Framework.Core;

public interface IFileSystem
{
    public bool Exists(FileUri uri);
    public Stream OpenRead(FileUri uri);
    public Stream OpenWrite(FileUri uri);

    public string ReadAllText(FileUri uri);
}