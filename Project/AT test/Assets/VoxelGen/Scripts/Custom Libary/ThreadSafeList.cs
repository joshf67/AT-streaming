using UnityEngine;
using System.Collections.Generic;
using System;

//===================== thread safe list template  ====================//

namespace CusHolder
{

    public class ThreadSafeList<T>
    {
        public List<T> _list = new List<T>();
        public object _sync = new object();

        //basic functions
        //return size of list
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        //return/set value at index in list
        public T this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                lock (_sync)
                {
                    _list[index] = value;
                }
            }
        }

        //return enumerator
        public IEnumerator<T> GetEnumerator()
        {
            lock (_sync)
            {
                return _list.GetEnumerator();
            }
        }

        //Add functions
        public void Add(T value)
        {
            lock (_sync)
            {
                _list.Add(value);
            }
        }

        public void Add(IEnumerable<T> value)
        {
            lock (_sync)
            {
                _list.AddRange(value);
            }
        }

        //Remove functions
        public void Remove(T value)
        {
            lock (_sync)
            {
                _list.Remove(value);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_sync)
            {
                _list.RemoveAt(index);
            }
        }

        public void RemoveAll(Predicate<T> value)
        {
            lock (_sync)
            {
                _list.RemoveAll(value);
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _list.Clear();
            }
        }

        //Find functions
        public T Find(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.Find(predicate);
            }
        }

        public bool Contains(T value)
        {
            lock (_sync)
            {
                return _list.Contains(value);
            }
        }

        public bool Exists(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.Exists(predicate);
            }
        }

        public int FindIndex(Predicate<T> predicate)
        {
            lock (_sync)
            {
                return _list.FindIndex(predicate);
            }
        }

    }

}