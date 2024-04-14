using System.Collections.Generic;
using System.Collections;

namespace Balls
{
    public class IterableList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        private List<T> _list = new List<T>();

        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;
        public bool IsSynchronized => true;
        public object SyncRoot => false;
        public bool IsFixedSize => false;

        public void Add(T item) => _list.Add(item);
        public void Clear() => _list.Clear();
        public bool Contains(T item) => _list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public int IndexOf(T item) => _list.IndexOf(item);
        public void Insert(int index, T item) => _list.Insert(index, item);
        public bool Remove(T item) => _list.Remove(item);
        public void RemoveAt(int index) => _list.RemoveAt(index);
        
        public IEnumerator<T> GetEnumerator() => new Enumerator(_list);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            public Enumerator(List<T> source)
            {
                _currentIndex = 0;
                _source = source;
                _current = default;
            }

            private int _currentIndex;
            private readonly List<T> _source;
            private T _current;

            public T Current => _current;
            object IEnumerator.Current => _current;

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                if (_currentIndex < _source.Count)
                {
                    _current = _source[_currentIndex];
                    _currentIndex++;
                    return true;
                }

                _current = default;
                return false;
            }

            void IEnumerator.Reset()
            {
                _currentIndex = 0;
                _current = default;
            }
        }
    }
}
