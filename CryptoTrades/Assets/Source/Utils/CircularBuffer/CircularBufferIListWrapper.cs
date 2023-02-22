using System;
using System.Collections;

namespace Utils
{
    public class CircularBufferIListWrapper<T> : IList
    {
        public CircularBufferIListWrapper(ObservableCircularBuffer<T> buffer)
        {
            Buffer = buffer;
        }

        public ObservableCircularBuffer<T> Buffer { get; }

        public object this[int index]
        {
            get => Buffer[index];
            set => Buffer[index] = (T) value;
        }
        public int Count => Buffer.Size;
        public IEnumerator GetEnumerator() => Buffer.GetEnumerator();

        // Since the ListView in UI Toolkit works with IList, we need all this nonsense.
        // It won't ever call into these methods, unless reordering is enabled.
        // It's fine, since I only need it to display the items and not to move them around.
        public void CopyTo(Array array, int index) => throw new NotImplementedException();
        public bool IsSynchronized => throw new NotImplementedException();
        public object SyncRoot => throw new NotImplementedException();
        public int Add(object value) => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public bool Contains(object value) => throw new NotImplementedException();
        public int IndexOf(object value) => throw new NotImplementedException();
        public void Insert(int index, object value) => throw new NotImplementedException();
        public void Remove(object value) => throw new NotImplementedException();
        public void RemoveAt(int index) => throw new NotImplementedException();
        public bool IsFixedSize => throw new NotImplementedException();
        public bool IsReadOnly => throw new NotImplementedException();
    }
}