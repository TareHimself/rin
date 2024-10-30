namespace rin.Core.DataStructures;

public interface IAeroxLinkedListNode<T>
{
    public T Value { get; set; }
    
    public IAeroxLinkedListNode<T>? Next { get; }
    
    public IAeroxLinkedListNode<T>? Previous { get; }
    public abstract bool RemoveNext();
    public abstract bool RemovePrevious();
    
    public abstract bool Remove();
    public abstract AeroxLinkedList<T> GetLinkedList(bool createIfCollected = false);
    
    public abstract AeroxLinkedList<T>? TryGetLinkedList();
}