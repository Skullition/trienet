// This code is distributed under MIT license. Copyright (c) 2013 George Mamaladze
// See license.txt or http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;

namespace Gma.DataStructures.StringSearch
{
    public abstract class TrieNodeBase<TValue>
    {
        protected abstract int KeyLength { get; }

        protected abstract IEnumerable<TValue> Values();

        protected abstract IEnumerable<TrieNodeBase<TValue>> Children();

        public void Add(string key, int position, TValue value)
        {
            if (key == null) 
                throw new ArgumentNullException("key");

            if (EndOfString(position, key))
            {
                AddValue(value);
                return;
            }

            TrieNodeBase<TValue> child = GetOrCreateChild(key[position]);
            child.Add(key, position + 1, value);
        }

        protected abstract void AddValue(TValue value);

        protected abstract TrieNodeBase<TValue> GetOrCreateChild(char key);

        protected virtual List<TValue> Retrieve(string query, int position)
            => EndOfString(position, query) ? ValuesDeep(): SearchDeep(query, position);

        protected virtual List<TValue> SearchDeep(string query, int position)
        {
            TrieNodeBase<TValue> nextNode = GetChildOrNull(query, position);
            return nextNode != null
                       ? nextNode.Retrieve(query, position + nextNode.KeyLength)
                       : new List<TValue>();
        }

        protected abstract TrieNodeBase<TValue> GetChildOrNull(string query, int position);

        private static bool EndOfString(int position, string text)
            => position >= text.Length;

        private List<TValue> ValuesDeep()
        {
            List<TValue> values = new List<TValue>();
            foreach (var t in Subtree())
                values.AddRange(t.Values());

            return values;
        }

        protected List<TrieNodeBase<TValue>> Subtree()
        {
            List<TrieNodeBase<TValue>> tree = new List<TrieNodeBase<TValue>>() { this };
            foreach (var child in Children())
                tree.AddRange(child.Subtree());

            return tree;
        }
    }
}