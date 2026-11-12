namespace Rin.Core.Extensions;

public static class CharacterExtensions
{
    public static bool IsPrintable(this char self)
    {
        return !char.IsControl(self) && self != ' ';
    }
}