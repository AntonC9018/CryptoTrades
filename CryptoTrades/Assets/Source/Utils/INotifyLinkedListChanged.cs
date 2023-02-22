namespace Utils
{
    public interface INotifyLinkedListChanged<T>
    {
        public event LinkedListChangedEventHandler<T> LinkedListChanged;
    }
    
    public enum LinkedListAction
    {
        PushFront,
        PushBack,
        PopFront,
        PopBack,
        ReplaceFront,
        ReplaceBack,
        Clear,
    }

    public readonly struct LinkedListChangedEventArgs<T>
    {
        public LinkedListAction Action { get; }
        public T ItemAdded { get; }

        public LinkedListChangedEventArgs(LinkedListAction action, T itemAdded)
        {
            Action = action;
            ItemAdded = itemAdded;
        }

        public static LinkedListChangedEventArgs<T> PushedBack(T itemAdded)
        {
            return new(LinkedListAction.PushBack, itemAdded);
        }
        
        public static LinkedListChangedEventArgs<T> PushedFront(T itemAdded)
        {
            return new(LinkedListAction.PushFront, itemAdded);
        }

        public static LinkedListChangedEventArgs<T> PoppedBack()
        {
            return new(LinkedListAction.PopBack, default);
        }
        
        public static LinkedListChangedEventArgs<T> PoppedFront()
        {
            return new(LinkedListAction.PopFront, default);
        }
        
        public static LinkedListChangedEventArgs<T> ReplacedBack(T itemAdded)
        {
            return new(LinkedListAction.ReplaceBack, itemAdded);
        }
        
        public static LinkedListChangedEventArgs<T> ReplacedFront(T itemAdded)
        {
            return new(LinkedListAction.ReplaceFront, itemAdded);
        }
        
        public static LinkedListChangedEventArgs<T> Cleared()
        {
            return new(LinkedListAction.Clear, default);
        }
        
        public override string ToString()
        {
            return $"Action: {Action}, ItemAdded: {ItemAdded}";
        }
    }
    
    public delegate void LinkedListChangedEventHandler<T>(INotifyLinkedListChanged<T> sender, LinkedListChangedEventArgs<T> e);
}