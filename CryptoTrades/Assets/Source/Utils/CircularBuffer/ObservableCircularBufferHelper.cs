using System;
using System.Linq;
using UnityEngine;

namespace Utils
{
    public static class ObservableCircularBufferHelper
    {
        public static void SubscribeReflectingCollectionChanged<TObserved, TOwn>(
            this ObservableCircularBuffer<TOwn> observer,
            ObservableCircularBuffer<TObserved> observable,
            Func<TObserved, TOwn> factory)
        {
            if (observable.Capacity != observer.Capacity)
            {
                throw new Exception("Capacities should match");
            }
            
            observable.CircularBufferChanged += (_, e) =>
            {
                switch (e.Action)
                {
                    case CircularBufferAction.PushBack:
                    {
                        observer.PushBackN(e.NewItems.Select(factory).ToArray());
                        break;
                    }
                    case CircularBufferAction.PushFront:
                    {
                        observer.PushFrontN(e.NewItems.Select(factory).ToArray());
                        break;
                    }
                    case CircularBufferAction.PopBack:
                    {
                        observer.PopBackN(e.OldItems.Count());
                        break;
                    }
                    case CircularBufferAction.PopFront:
                    {
                        observer.PopFront();
                        break;
                    }
                    case CircularBufferAction.SetAtIndex:
                    {
                        observer[e.Index] = factory(e.NewItems[0]);
                        break;
                    }
                    case CircularBufferAction.Clear:
                    {
                        observer.Clear();
                        break;
                    }
                }                
            };
        }
    }
}