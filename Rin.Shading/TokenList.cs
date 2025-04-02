namespace Rin.Shading;

public sealed class TokenList
{
    private readonly LinkedList<Token> _list;
    private Token? _lastBack;
    private Token? _lastFont;

    public TokenList() : this([])
    {
    }

    public TokenList(IEnumerable<Token> tokens)
    {
        _list = new LinkedList<Token>(tokens);
        _lastFont = _list.FirstOrDefault();
        _lastBack = _list.LastOrDefault();
    }

    public Exception CreateException(string message, Token token)
    {
        throw new NotImplementedException();
        //return new ExceptionWithDebug(token.DebugInfo,message);
    }

    public void ThrowExpectedInput()
    {
        if (_lastFont != null) throw new Exception($"Expected Input After Token {_lastFont}");
        throw new Exception("Expected Input");
    }

    public Token RemoveFront()
    {
        if (Empty()) ThrowExpectedInput();
        var a = _lastFont = Front();
        _list.RemoveFirst();
        return a;
    }

    public Token RemoveBack()
    {
        if (Empty()) ThrowExpectedInput();

        var a = _lastBack = Back();
        _list.RemoveLast();
        return a;
    }

    // public TokenList ExpectFront(TokenType type)
    // {
    //     if (Empty())
    //     {
    //         ThrowExpectedInput();
    //     }
    //
    //     var a = Front();
    //     
    //     if(a.Type != type) throw CreateException("Unexpected input",a);
    //     return this;
    // }

    public TokenList ExpectFront(params TokenType[] type)
    {
        if (Empty()) ThrowExpectedInput();

        var a = Front();

        if (!type.Contains(a.Type))
            throw CreateException(
                "Expected " + "[" + type.Aggregate("", (c, d) => $" {d.ToString()} ") + "]" +
                $" but got {a.Type.ToString()}", a);
        return this;
    }

    // public TokenList ExpectBack(TokenType type)
    // {
    //     if (Empty())
    //     {
    //         ThrowExpectedInput();
    //     }
    //
    //     var a = Back();
    //     
    //     if(a.Type != type) throw CreateException("Unexpected input",a);
    //     return this;
    // }
    public TokenList ExpectBack(params TokenType[] type)
    {
        if (Empty()) ThrowExpectedInput();

        var a = Back();

        if (!type.Contains(a.Type))
            throw CreateException(
                "Expected " + "[" + type.Aggregate("", (c, d) => $" {d.ToString()} ") + "]" +
                $" but got {a.Type.ToString()}", a);
        return this;
    }

    public Token Front()
    {
        if (Empty()) ThrowExpectedInput();

        return _list.First();
    }

    public Token Back()
    {
        if (Empty()) ThrowExpectedInput();

        return _list.Last();
    }

    public int Size()
    {
        return _list.Count;
    }

    public TokenList InsertFront(TokenList other)
    {
        while (other.NotEmpty()) _list.AddFirst(other.RemoveBack());

        return this;
    }

    public TokenList InsertFront(Token token)
    {
        _list.AddFirst(token);
        _lastFont = token;
        return this;
    }

    public TokenList InsertBack(TokenList other)
    {
        while (other.NotEmpty()) _list.AddLast(other.RemoveFront());
        _lastBack = _list.LastOrDefault();
        return this;
    }

    public TokenList InsertBack(Token other)
    {
        _list.AddLast(other);
        _lastBack = other;
        return this;
    }

    public TokenList Clear()
    {
        _list.Clear();
        return this;
    }

    public TokenList Clone()
    {
        return new TokenList(_list);
    }

    public bool Empty()
    {
        return Size() == 0;
    }

    public bool NotEmpty()
    {
        return Size() > 0;
    }
}