﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MPack
{
    public sealed class MPackArray : MPack, IList<MPack>
    {
        public override object Value => _collection.AsReadOnly();
        public override MPackType ValueType => MPackType.Array;

        private readonly List<MPack> _collection = new();

        public MPackArray()
        {
        }

        public MPackArray(IEnumerable<MPack> seed)
        {
            foreach (var v in seed)
                _collection.Add(v);
        }

        public override MPack this[int index]
        {
            get { return _collection[index]; }
            set { _collection[index] = value; }
        }

        public int Count
        {
            get { return _collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(MPack item)
        {
            _collection.Add(item);
        }

        public void Clear()
        {
            _collection.Clear();
        }

        public bool Contains(MPack item)
        {
            return _collection.Contains(item);
        }

        public void CopyTo(MPack[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public int IndexOf(MPack item)
        {
            return _collection.IndexOf(item);
        }

        public void Insert(int index, MPack item)
        {
            _collection.Insert(index, item);
        }

        public bool Remove(MPack item)
        {
            return _collection.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _collection.RemoveAt(index);
        }

        public IEnumerator<MPack> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        public override string ToString()
        {
            return string.Join(",", this.Select(v => v.ToString()));
        }
    }
}