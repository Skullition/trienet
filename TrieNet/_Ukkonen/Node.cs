using System;
using System.Collections.Generic;
using System.Linq;

namespace Gma.DataStructures.StringSearch
{
    
    
    internal class Node<T>
    {
        private readonly IDictionary<char, Edge<T>> _edges;
        private readonly HashSet<T> _data;

        public Node()
        {
            _edges = new CharDictionary<T>();
            Suffix = null;
            _data = new HashSet<T>();
        }

       

        public List<T> GetData()
        {
            List<T> t = _data.ToList();

            foreach (var n in _edges.Values)
            {
                List<T> data = n.Target.GetData();
                foreach (var d in data)
                {
                    if (!_data.Contains(d))
                        t.Add(d);
                }
                    
            }
           
            return t;
        }

        public void AddRef(T value)
        {
            if (_data.Contains(value))
                return;

            _data.Add(value);
            //  add this reference to all the suffixes as well
            var iter = Suffix;
            while (iter != null)
            {
                if (iter._data.Contains(value))
                    break;

                iter.AddRef(value);
                iter = iter.Suffix;
            }
        }

        public void AddEdge(char ch, Edge<T> e)
        {
            _edges[ch] = e;
        }

        public Edge<T> GetEdge(char ch)
        {
            Edge<T> result;
            _edges.TryGetValue(ch, out result);
            return result;
        }

        public Node<T> Suffix { get; set; }
    }
}