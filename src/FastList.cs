using System;

namespace Balls
{
    public struct FastList<T>
    {
        public FastList(uint cap = 3u)
        {
            _cap = cap;
            _data = new T[cap];
            _len = 0;
        }
        
        private T[] _data;
        private uint _cap;
        private uint _len;
        
        public uint Length => _len;
        public uint Capcity => _cap;
        
        private void ExtendCap()
        {
            uint nc = _cap * 2u;
            T[] nd = new T[nc];
            ((Span<T>)_data).Slice(0, (int)_len).CopyTo(nd);
            _data = nd;
            _cap = nc;
        }
        
        public void RemoveAt(int index)
        {
            _data[index] = _data[_len - 1];
            _len--;
        }
        public void Add(T item)
        {
            if (_len == _cap)
            {
                ExtendCap();
            }
            
            _data[_len] = item;
            _len++;
        }
        public bool Remove(T item)
        {
            for (int i = 0; i < _len; i++)
            {
                if (item.Equals(_data[i]))
                {
                    RemoveAt(i);
                    return true;
                }
            }
            
            return false;
        }
        public void Clear() => _len = 0;
        
        public bool Contains(T item)
        {
            for (int i = 0; i < _len; i++)
            {
                if (item.Equals(_data[i]))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        public Span<T> AsSpan() => ((Span<T>)_data).Slice(0, (int)_len);
    }
}