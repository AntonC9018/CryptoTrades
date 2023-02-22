using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public enum CircularBufferAction
    {
        PushBack,
        PopBack,
        PushFront,
        PopFront,
        SetAtIndex,
        Clear,
    }

    public class CircularBufferChangedEventArgs<T>
    {
        public CircularBufferAction Action { get; }
        public int Index { get; }

        public bool Removed => OldItems?.Count != 0;
        public IReadOnlyList<T> OldItems { get; }
        public IReadOnlyList<T> NewItems { get; }
        
        private CircularBufferChangedEventArgs(CircularBufferAction action,
            int index = -1,
            IReadOnlyList<T> newItems = default,
            IReadOnlyList<T> oldItems = default)
        {
            Action = action;
            Index = index;
            NewItems = newItems;
            OldItems = oldItems;
        }
        
        public static CircularBufferChangedEventArgs<T> PushedBack_PopFront(IReadOnlyList<T> itemsAdded, IReadOnlyList<T> itemsRemoved)
        {
            return new(CircularBufferAction.PushBack, -1, itemsAdded, itemsRemoved);
        }
        public static CircularBufferChangedEventArgs<T> PushedBack_PopFront(T itemAdded, T itemRemoved) => PushedBack_PopFront(new[]{itemAdded}, new[]{itemRemoved});
        public static CircularBufferChangedEventArgs<T> PushedBack(IReadOnlyList<T> itemsAdded)
        {
            return new(CircularBufferAction.PushBack, -1, itemsAdded, default);
        }
        public static CircularBufferChangedEventArgs<T> PushedBack(T itemAdded) => PushedBack(new[]{itemAdded});
        public static CircularBufferChangedEventArgs<T> PushedFront_PopBack(IReadOnlyList<T> itemsAdded, IReadOnlyList<T> itemsRemoved)
        {
            return new(CircularBufferAction.PushFront, -1, itemsAdded, itemsRemoved);
        }
        public static CircularBufferChangedEventArgs<T> PushedFront_PopBack(T itemAdded, T itemRemoved) => PushedFront_PopBack(new[]{itemAdded}, new[]{itemRemoved});
        public static CircularBufferChangedEventArgs<T> PushedFront(IReadOnlyList<T> itemsAdded)
        {
            return new(CircularBufferAction.PushFront, -1, itemsAdded, default);
        }
        public static CircularBufferChangedEventArgs<T> PushedFront(T itemAdded) => PushedFront(new[]{itemAdded});
        public static CircularBufferChangedEventArgs<T> PoppedBack(IReadOnlyList<T> itemRemoved)
        {
            return new(CircularBufferAction.PopBack, -1, default, itemRemoved);
        }
        public static CircularBufferChangedEventArgs<T> PoppedBack(T itemRemoved) => PoppedBack(new[]{itemRemoved});
        public static CircularBufferChangedEventArgs<T> PoppedFront(IReadOnlyList<T> itemsRemoved)
        {
            return new(CircularBufferAction.PopFront, -1, default, itemsRemoved);
        }
        public static CircularBufferChangedEventArgs<T> PoppedFront(T itemRemoved) => PoppedFront(new[]{itemRemoved});
        public static CircularBufferChangedEventArgs<T> SetAtIndex(int index, T itemAdded, T itemRemoved)
        {
            return new(CircularBufferAction.SetAtIndex, index, new[]{itemAdded}, new[]{itemRemoved});
        }
        public static CircularBufferChangedEventArgs<T> Cleared()
        {
            return new(CircularBufferAction.Clear, -1, default, default);
        }
    }
    
    public delegate void CircularBufferChangedEventHandler<T>(object sender, CircularBufferChangedEventArgs<T> e);
    
    public interface INotifyCircularBufferChanged<T>
    {
        public event CircularBufferChangedEventHandler<T> CircularBufferChanged;            
    }
}