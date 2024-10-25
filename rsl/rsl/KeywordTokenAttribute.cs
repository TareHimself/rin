namespace rsl;

public class KeywordTokenAttribute(string keyword) : Attribute
{
    public string Keyword = keyword;
}