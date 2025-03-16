using System.Globalization;
using JetBrains.Annotations;

namespace Rin.Engine.Graphics;

public struct Version : IFormattable
{
    [PublicAPI] public uint Major = 0;
    [PublicAPI] public uint Minor = 0;
    [PublicAPI] public uint Patch = 0;

    public Version()
    {
        
    }
    
    public Version(uint major, uint minor, uint patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }
    
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{Major.ToString(format, formatProvider)}{separator} {Minor.ToString(format, formatProvider)}{separator} {Patch.ToString(format, formatProvider)}>";
    }
}