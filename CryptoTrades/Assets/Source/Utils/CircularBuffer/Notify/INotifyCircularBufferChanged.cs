using System.Linq;

namespace Utils
{
    public interface INotifyCircularBufferChanged<T>
    {
        public event CircularBufferChangedEventHandler<T> CircularBufferChanged;            
    }
}