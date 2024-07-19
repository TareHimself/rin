using System.Collections;

namespace aerox.Runtime.DataStructures;

public class AeroxLinkedListEnumerator<T> : IEnumerator<IAeroxLinkedListNode<T>>
{
    private IAeroxLinkedListNode<T>? _start;
    private IAeroxLinkedListNode<T>? _current;
    private bool _forward;
    private bool _isReset = true;
    public AeroxLinkedListEnumerator(IAeroxLinkedListNode<T>? start, bool forward = true)
    {
        _start = start;
        _current = _start;
        _forward = forward;
    }

    public bool MoveNext()
    {
        if (_isReset)
        {
            _isReset = false;
            if (_current == null) return false;
            return true;
        }
        
        var next = _forward ? _current?.Next : _current?.Previous;
        if (next == null) return false;
        _current = next;
        return true;
    }

    public void Reset()
    {
        _current = _start;
        _isReset = true;
    }

    public IAeroxLinkedListNode<T> Current => _current!;

    object IEnumerator.Current => _current!;

    public void Dispose()
    {
        
    }
}