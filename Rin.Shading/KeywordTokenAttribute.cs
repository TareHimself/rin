namespace Rin.Shading;

public class KeywordTokenAttribute(string keyword) : Attribute
{
    public string Keyword = keyword;
}