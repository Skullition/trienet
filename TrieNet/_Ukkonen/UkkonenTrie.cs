using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gma.DataStructures.StringSearch
{
    public class UkkonenTrie<T> : ITrie<T>
    {
        private readonly int _minSuffixLength;

        //The root of the suffix tree
        private readonly Node<T> _root;

        //The last leaf that was added during the update operation
        private Node<T> _activeLeaf;

        public UkkonenTrie() : this(0)
        {
        }

        public UkkonenTrie(int minSuffixLength) 
        {
            _minSuffixLength = minSuffixLength;
            _root = new Node<T>();
            _activeLeaf = _root;
        }

        public List<T> Retrieve(string word)
        {
            if (word.Length < _minSuffixLength) return null;
            var tmpNode = SearchNode(word);

            if (tmpNode is null)
                return null;

            return tmpNode.GetData();
        }

        /**
         * Returns the tree NodeA<T> (if present) that corresponds to the given string.
         */
        private Node<T> SearchNode(string word)
        {
            /*
             * Verifies if exists a path from the root to a NodeA<T> such that the concatenation
             * of all the labels on the path is a superstring of the given word.
             * If such a path is found, the last NodeA<T> on it is returned.
             */
            var currentNode = _root;

            for (var i = 0; i < word.Length; ++i)
            {
                string ch = word[i];
                // follow the EdgeA<T> corresponding to this char
                Edge<T> currentEdge = currentNode.GetEdge(ch);
                if (null == currentEdge)
                {
                    // there is no EdgeA<T> starting with this char
                    return null;
                }

                string label = currentEdge.Label;
                int lenToMatch = Math.Min(word.Length - i, label.Length);

                if (!word.AsSpan(i, lenToMatch).SequenceEqual(label.AsSpan(0, lenToMatch)))
                {
                    // the label on the EdgeA<T> does not correspond to the one in the string to search
                    return null;
                }

                if (label.Length >= word.Length - i)
                    return currentEdge.Target;

                // advance to next NodeA<T>
                currentNode = currentEdge.Target;
                i += lenToMatch - 1;
            }

            return null;
        }

        public void Add(string key, T value)
        {
            // reset activeLeaf
            _activeLeaf = _root;

            string remainder = key;
            Node<T> s = _root;

            // proceed with tree construction (closely related to procedure in
            // Ukkonen's paper)
            string text = string.Empty;
            // iterate over the string, one char at a time
            for (var i = 0; i < remainder.Length; i++)
            {
                // line 6
                text += remainder[i];
                // use intern to make sure the resulting string is in the pool.
                //TODO Check if needed
                //text = text.Intern();

                // line 7: update the tree with the new transitions due to this new char
                Node<T> active = Update(s, text, remainder.AsSpan(i), value, out ReadOnlySpan<char> sp);
                // line 8: make sure the active Tuple is canonical
                active = Canonize(active, sp, out ReadOnlySpan<char> sp2);

                s = active;
                text = sp2.ToString();
            }

            // add leaf suffix link, is necessary
            if (null == _activeLeaf.Suffix && _activeLeaf != _root && _activeLeaf != s)
            {
                _activeLeaf.Suffix = s;
            }

        }

        /**
         * Tests whether the string stringPart + t is contained in the subtree that has inputs as root.
         * If that's not the case, and there exists a path of edges e1, e2, ... such that
         *     e1.label + e2.label + ... + $end = stringPart
         * and there is an EdgeA<T> g such that
         *     g.label = stringPart + rest
         * 
         * Then g will be split in two different edges, one having $end as label, and the other one
         * having rest as label.
         *
         * @param inputs the starting NodeA<T>
         * @param stringPart the string to search
         * @param t the following character
         * @param remainder the remainder of the string to add to the index
         * @param value the value to add to the index
         * @return a Tuple containing
         *                  true/false depending on whether (stringPart + t) is contained in the subtree starting in inputs
         *                  the last NodeA<T> that can be reached by following the path denoted by stringPart starting from inputs
         *         
         */
        private static bool TestAndSplit(Node<T> inputs, ReadOnlySpan<char> stringPart, char t, ReadOnlySpan<char> remainder, T value, out Node<T> node)
        {
            // descend the tree as far as possible
            Node<T> s = Canonize(inputs, stringPart, out ReadOnlySpan<char> str);

            if (!str.IsEmpty)
            {
                var g = s.GetEdge(str[0]);

                var label = g.Label;
                // must see whether "str" is substring of the label of an EdgeA<T>
                if (label.Length > str.Length && label[str.Length] == t)
                {
                    node = s;
                    return true;
                }
                // need to split the EdgeA<T>
                string newlabel = label.Substring(str.Length);
                //assert (label.startsWith(str));

                // build a new NodeA<T>
                var r = new Node<T>();
                // build a new EdgeA<T>
                var newedge = new Edge<T>(str.ToString(), r);

                g.Label = newlabel;

                // link s -> r
                r.AddEdge(newlabel[0], g);
                s.AddEdge(str[0], newedge);

                node = r;
                return false;
            }

            Edge<T> e = s.GetEdge(t);
            node = s;

            if (null == e)
            {
                // if there is no t-transtion from s
                return false;
            }
            if (remainder == e.Label)
            {
                // update payload of destination NodeA<T>
                e.Target.AddRef(value);
                return true;
            }
            if (remainder.StartsWith(e.Label))
            {
                return true;
            }
            if (!e.Label.AsSpan().StartsWith(remainder))
            {
                return true;
            }
            // need to split as above
            var newNode = new Node<T>();
            newNode.AddRef(value);

            var newEdge = new Edge<T>(remainder.ToString(), newNode);
            e.Label = e.Label.Substring(remainder.Length);
            newNode.AddEdge(e.Label[0], e);
            s.AddEdge(t, newEdge);
            return false;
            // they are different words. No prefix. but they may still share some common substr
        }

        /**
         * Return a (NodeA<T>, string) (n, remainder) Tuple such that n is a farthest descendant of
         * s (the input NodeA<T>) that can be reached by following a path of edges denoting
         * a prefix of inputstr and remainder will be string that must be
         * appended to the concatenation of labels from s to n to get inpustr.
         */
        private static Node<T> Canonize(Node<T> s, ReadOnlySpan<char> inputstr, out ReadOnlySpan<char> sp)
        {

            if (inputstr.IsEmpty)
            {
                sp = inputstr;
                return s;
            }
            
            var currentNode = s;
            var str = inputstr;
            var g = s.GetEdge(str[0]);
            // descend the tree as long as a proper label is found
            while (g != null && str.StartsWith(g.Label))
            {
                str = str.Slice(g.Label.Length);
                currentNode = g.Target;
                if (str.Length > 0)
                {
                    g = currentNode.GetEdge(str[0]);
                }
            }

            sp = str;
            return currentNode;
        }

        /**
         * Updates the tree starting from inputNode and by adding stringPart.
         * 
         * Returns a reference (NodeA<T>, string) Tuple for the string that has been added so far.
         * This means:
         * - the NodeA<T> will be the NodeA<T> that can be reached by the longest path string (S1)
         *   that can be obtained by concatenating consecutive edges in the tree and
         *   that is a substring of the string added so far to the tree.
         * - the string will be the remainder that must be added to S1 to get the string
         *   added so far.
         * 
         * @param inputNode the NodeA<T> to start from
         * @param stringPart the string to add to the tree
         * @param rest the rest of the string
         * @param value the value to add to the index
         */
        private Node<T> Update(Node<T> inputNode, string stringPart, ReadOnlySpan<char> rest, T value, out ReadOnlySpan<char> sp)
        {
            var s = inputNode;
            ReadOnlySpan<char> tempstr = stringPart;
            var newChar = stringPart[^1];

            // line 1
            var oldroot = _root;

            // line 1b
            bool endpoint = TestAndSplit(s, tempstr[0..^1], newChar, rest, value, out Node<T> r);

            // line 2
            while (!endpoint)
            {
                // line 3
                var tempEdge = r.GetEdge(newChar);
                Node<T> leaf;
                if (null != tempEdge)
                {
                    // such a NodeA<T> is already present. This is one of the main differences from Ukkonen's case:
                    // the tree can contain deeper nodes at this stage because different strings were added by previous iterations.
                    leaf = tempEdge.Target;
                }
                else
                {
                    // must build a new leaf
                    leaf = new Node<T>();
                    leaf.AddRef(value);
                    var newedge = new Edge<T>(rest.ToString(), leaf);
                    r.AddEdge(newChar, newedge);
                }

                // update suffix link for newly created leaf
                if (_activeLeaf != _root)
                {
                    _activeLeaf.Suffix = leaf;
                }
                _activeLeaf = leaf;

                // line 4
                if (oldroot != _root)
                {
                    oldroot.Suffix = r;
                }

                // line 5
                oldroot = r;

                // line 6
                if (null == s.Suffix)
                {
                    // root NodeA<T>
                    //TODO Check why assert
                    //assert (root == s);
                    // this is a special case to handle what is referred to as NodeA<T> _|_ on the paper
                    tempstr = tempstr.Slice(1);
                }
                else
                {
                    Node<T> canret = Canonize(s.Suffix, SafeCutLastChar(tempstr), out ReadOnlySpan<char> spa);
                    s = canret;
                    // use intern to ensure that tempstr is a reference from the string pool
                    Span<char> buffer = spa.Length < 512 ? stackalloc char[spa.Length + 1] : new char[spa.Length + 1];
                    spa.CopyTo(buffer);
                    buffer[^1] = tempstr[^1];
                    tempstr = new string(buffer); //TODO .intern();
                }

                // line 7
                endpoint = TestAndSplit(s, SafeCutLastChar(tempstr), newChar, rest, value, out r);
            }

            // line 8
            if (oldroot != _root)
            {
                oldroot.Suffix = r;
            }

            sp = tempstr;
            return s;
        }

        private static ReadOnlySpan<char> SafeCutLastChar(ReadOnlySpan<char> seq)
            => seq.IsEmpty ? seq : seq[0..^1];
    }
}